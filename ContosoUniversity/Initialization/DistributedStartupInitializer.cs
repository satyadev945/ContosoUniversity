using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace ContosoUniversity.Initialization
{
    /// <summary>
    /// Replaces the instance-local Global.asax Application_Start initialization.
    ///
    /// - Shared settings come from AWS Systems Manager Parameter Store
    ///   (path prefix from APP_CONFIG_PARAMETER_PATH, default /contosouniversity/).
    /// - Cluster-wide one-time work is coordinated with a DynamoDB conditional-write lock
    ///   (table INIT_LOCK_TABLE_NAME, default ContosoUniversityInitLock) so that only one
    ///   scaled instance performs it while the others wait for completion.
    /// - Per-process work (e.g. EF6 interceptor registration) is still run on every instance
    ///   because it is in-memory state, but only after shared configuration has been loaded.
    ///
    /// Set DISTRIBUTED_INIT_ENABLED=false to disable the AWS calls (e.g. local development).
    /// </summary>
    public static class DistributedStartupInitializer
    {
        private const string InitKey = "ApplicationStartup";

        public static IDictionary<string, string> LoadedParameters { get; private set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static async Task InitializeAsync(
            Func<Task> clusterWideInitialization,
            Action perInstanceInitialization,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var enabled = Environment.GetEnvironmentVariable("DISTRIBUTED_INIT_ENABLED");
            if (!string.Equals(enabled, "false", StringComparison.OrdinalIgnoreCase))
            {
                await LoadParametersAsync(cancellationToken).ConfigureAwait(false);
                await RunWithDistributedLockAsync(clusterWideInitialization, cancellationToken).ConfigureAwait(false);
            }
            else if (clusterWideInitialization != null)
            {
                await clusterWideInitialization().ConfigureAwait(false);
            }

            perInstanceInitialization?.Invoke();
        }

        private static RegionEndpoint GetRegion()
        {
            var region = Environment.GetEnvironmentVariable("AWS_REGION")
                         ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
            return string.IsNullOrEmpty(region) ? null : RegionEndpoint.GetBySystemName(region);
        }

        private static async Task LoadParametersAsync(CancellationToken ct)
        {
            var path = Environment.GetEnvironmentVariable("APP_CONFIG_PARAMETER_PATH") ?? "/contosouniversity/";
            var region = GetRegion();
            using (var ssm = region == null ? new AmazonSimpleSystemsManagementClient() : new AmazonSimpleSystemsManagementClient(region))
            {
                string nextToken = null;
                do
                {
                    var response = await ssm.GetParametersByPathAsync(new GetParametersByPathRequest
                    {
                        Path = path,
                        Recursive = true,
                        WithDecryption = true,
                        NextToken = nextToken
                    }, ct).ConfigureAwait(false);

                    foreach (var p in response.Parameters)
                    {
                        LoadedParameters[p.Name.Substring(path.Length).Trim('/')] = p.Value;
                    }
                    nextToken = response.NextToken;
                } while (!string.IsNullOrEmpty(nextToken));
            }
        }

        private static async Task RunWithDistributedLockAsync(Func<Task> work, CancellationToken ct)
        {
            if (work == null) return;

            var table = Environment.GetEnvironmentVariable("INIT_LOCK_TABLE_NAME") ?? "ContosoUniversityInitLock";
            var version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "1";
            var lockId = InitKey + ":" + version;
            var owner = Environment.MachineName + ":" + Guid.NewGuid().ToString("N");
            var region = GetRegion();
            var leaseSeconds = 300;
            var deadline = DateTime.UtcNow.AddSeconds(leaseSeconds);

            using (var ddb = region == null ? new AmazonDynamoDBClient() : new AmazonDynamoDBClient(region))
            {
                while (DateTime.UtcNow < deadline)
                {
                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    try
                    {
                        // Acquire if no item exists, or the previous lease has expired and it is not completed.
                        await ddb.PutItemAsync(new PutItemRequest
                        {
                            TableName = table,
                            Item = new Dictionary<string, AttributeValue>
                            {
                                ["LockId"] = new AttributeValue { S = lockId },
                                ["Owner"] = new AttributeValue { S = owner },
                                ["Status"] = new AttributeValue { S = "IN_PROGRESS" },
                                ["ExpiresAt"] = new AttributeValue { N = (now + leaseSeconds).ToString() }
                            },
                            ConditionExpression = "attribute_not_exists(LockId) OR (#s <> :done AND ExpiresAt < :now)",
                            ExpressionAttributeNames = new Dictionary<string, string> { ["#s"] = "Status" },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                            {
                                [":done"] = new AttributeValue { S = "COMPLETED" },
                                [":now"] = new AttributeValue { N = now.ToString() }
                            }
                        }, ct).ConfigureAwait(false);
                    }
                    catch (ConditionalCheckFailedException)
                    {
                        // Another instance owns the lock or has completed initialization.
                        var item = await ddb.GetItemAsync(table,
                            new Dictionary<string, AttributeValue> { ["LockId"] = new AttributeValue { S = lockId } },
                            true, ct).ConfigureAwait(false);
                        if (item.Item != null && item.Item.TryGetValue("Status", out var status) && status.S == "COMPLETED")
                        {
                            return;
                        }
                        await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
                        continue;
                    }

                    try
                    {
                        await work().ConfigureAwait(false);
                        await ddb.UpdateItemAsync(new UpdateItemRequest
                        {
                            TableName = table,
                            Key = new Dictionary<string, AttributeValue> { ["LockId"] = new AttributeValue { S = lockId } },
                            UpdateExpression = "SET #s = :done",
                            ConditionExpression = "Owner = :owner",
                            ExpressionAttributeNames = new Dictionary<string, string> { ["#s"] = "Status" },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                            {
                                [":done"] = new AttributeValue { S = "COMPLETED" },
                                [":owner"] = new AttributeValue { S = owner }
                            }
                        }, ct).ConfigureAwait(false);
                        return;
                    }
                    catch
                    {
                        // Release the lock so another instance can retry.
                        await ddb.DeleteItemAsync(table,
                            new Dictionary<string, AttributeValue> { ["LockId"] = new AttributeValue { S = lockId } },
                            ct).ConfigureAwait(false);
                        throw;
                    }
                }

                throw new TimeoutException("Timed out waiting for distributed application initialization.");
            }
        }
    }
}
