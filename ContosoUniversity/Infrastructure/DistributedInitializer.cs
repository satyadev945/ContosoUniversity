using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace ContosoUniversity.Infrastructure
{
    /// <summary>
    /// Replaces instance-specific Global.asax Application_Start initialization for scaled AWS deployments.
    /// - Centralized configuration/state lives in AWS Systems Manager Parameter Store.
    /// - A DynamoDB conditional write acts as a distributed lock so that shared (cluster-wide)
    ///   initialization runs exactly once, no matter how many instances start concurrently.
    ///
    /// Environment variables:
    ///   INIT_PARAMETER_PREFIX   Parameter Store prefix (e.g. /contoso/prod). When unset, distributed init is skipped.
    ///   INIT_LOCK_TABLE         DynamoDB table (partition key "LockId", string). Default: ContosoUniversityInitLocks
    ///   INIT_VERSION            Version marker for the initialization (default "1"). Bump to force re-run.
    ///   INIT_LOCK_TTL_SECONDS   Lock lease duration (default 120).
    /// </summary>
    public static class DistributedInitializer
    {
        public const string ParameterPrefixVariable = "INIT_PARAMETER_PREFIX";
        public const string LockTableVariable = "INIT_LOCK_TABLE";
        public const string VersionVariable = "INIT_VERSION";
        public const string LockTtlVariable = "INIT_LOCK_TTL_SECONDS";

        private const string LockId = "application-initialization";

        /// <summary>
        /// Runs <paramref name="sharedInitialization"/> once across all instances. Returns true when this
        /// instance executed it, false when it was already completed (or distributed init is not configured).
        /// </summary>
        public static async Task<bool> RunOnceAsync(Func<Task> sharedInitialization)
        {
            var prefix = Environment.GetEnvironmentVariable(ParameterPrefixVariable);
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return false;
            }

            prefix = "/" + prefix.Trim().Trim('/');
            var table = Environment.GetEnvironmentVariable(LockTableVariable) ?? "ContosoUniversityInitLocks";
            var version = Environment.GetEnvironmentVariable(VersionVariable) ?? "1";
            var ttl = int.TryParse(Environment.GetEnvironmentVariable(LockTtlVariable), out var t) ? t : 120;
            var markerName = prefix + "/initialized-version";
            var owner = Environment.MachineName + ":" + Guid.NewGuid().ToString("N");

            using (var ssm = new AmazonSimpleSystemsManagementClient())
            using (var ddb = new AmazonDynamoDBClient())
            {
                if (await IsCompletedAsync(ssm, markerName, version))
                {
                    return false;
                }

                // Wait for lock (or for another instance to finish).
                var deadline = DateTime.UtcNow.AddSeconds(ttl * 2);
                while (!await TryAcquireLockAsync(ddb, table, owner, ttl))
                {
                    if (await IsCompletedAsync(ssm, markerName, version))
                    {
                        return false;
                    }
                    if (DateTime.UtcNow > deadline)
                    {
                        throw new TimeoutException("Timed out waiting for distributed initialization lock.");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }

                try
                {
                    // Double-check after acquiring the lock.
                    if (await IsCompletedAsync(ssm, markerName, version))
                    {
                        return false;
                    }

                    if (sharedInitialization != null)
                    {
                        await sharedInitialization();
                    }

                    await ssm.PutParameterAsync(new PutParameterRequest
                    {
                        Name = markerName,
                        Value = version,
                        Type = ParameterType.String,
                        Overwrite = true
                    });
                    Console.WriteLine("{\"level\":\"info\",\"message\":\"Distributed initialization completed\",\"version\":\"" + version + "\"}");
                    return true;
                }
                finally
                {
                    await ReleaseLockAsync(ddb, table, owner);
                }
            }
        }

        /// <summary>Reads a centralized configuration value from Parameter Store (null when missing).</summary>
        public static async Task<string> GetParameterAsync(string name, bool decrypt = true)
        {
            using (var ssm = new AmazonSimpleSystemsManagementClient())
            {
                try
                {
                    var response = await ssm.GetParameterAsync(new GetParameterRequest { Name = name, WithDecryption = decrypt });
                    return response.Parameter.Value;
                }
                catch (ParameterNotFoundException)
                {
                    return null;
                }
            }
        }

        private static async Task<bool> IsCompletedAsync(IAmazonSimpleSystemsManagement ssm, string name, string version)
        {
            try
            {
                var response = await ssm.GetParameterAsync(new GetParameterRequest { Name = name });
                return string.Equals(response.Parameter.Value, version, StringComparison.Ordinal);
            }
            catch (ParameterNotFoundException)
            {
                return false;
            }
        }

        private static async Task<bool> TryAcquireLockAsync(IAmazonDynamoDB ddb, string table, string owner, int ttlSeconds)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            try
            {
                await ddb.PutItemAsync(new PutItemRequest
                {
                    TableName = table,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["LockId"] = new AttributeValue { S = LockId },
                        ["Owner"] = new AttributeValue { S = owner },
                        ["ExpiresAt"] = new AttributeValue { N = (now + ttlSeconds).ToString() }
                    },
                    ConditionExpression = "attribute_not_exists(LockId) OR ExpiresAt < :now",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":now"] = new AttributeValue { N = now.ToString() }
                    }
                });
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
        }

        private static async Task ReleaseLockAsync(IAmazonDynamoDB ddb, string table, string owner)
        {
            try
            {
                await ddb.DeleteItemAsync(new DeleteItemRequest
                {
                    TableName = table,
                    Key = new Dictionary<string, AttributeValue> { ["LockId"] = new AttributeValue { S = LockId } },
                    ConditionExpression = "Owner = :owner",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":owner"] = new AttributeValue { S = owner }
                    }
                });
            }
            catch (ConditionalCheckFailedException)
            {
                // Lease expired and was taken over by another instance; nothing to release.
            }
        }
    }
}
