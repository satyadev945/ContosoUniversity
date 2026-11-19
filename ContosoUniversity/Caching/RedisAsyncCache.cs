using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ContosoUniversity.Caching
{
    /// <summary>
    /// GCP Memorystore (Redis) backed async cache. Configuration comes from the environment (12-factor):
    ///   REDIS_CONNECTION_STRING            - full StackExchange.Redis connection string (takes precedence), or
    ///   MEMORYSTORE_REDIS_HOST / REDIS_HOST (+ REDIS_PORT, REDIS_AUTH_STRING) - Memorystore instance address.
    /// When no Redis endpoint is configured, or Redis is unavailable, the cache transparently falls back
    /// to invoking the factory (database) so the application keeps working.
    /// </summary>
    public sealed class RedisAsyncCache : IAsyncCache
    {
        private const string KeyPrefix = "contoso:";

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        private static readonly object ConnectLock = new object();
        private static Task<ConnectionMultiplexer> connectionTask;

        public static readonly RedisAsyncCache Instance = new RedisAsyncCache();

        private RedisAsyncCache()
        {
        }

        private static string BuildConnectionString()
        {
            var connectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }

            var host = Environment.GetEnvironmentVariable("MEMORYSTORE_REDIS_HOST")
                       ?? Environment.GetEnvironmentVariable("REDIS_HOST");
            if (string.IsNullOrWhiteSpace(host))
            {
                return null;
            }

            var port = Environment.GetEnvironmentVariable("REDIS_PORT");
            if (string.IsNullOrWhiteSpace(port))
            {
                port = "6379";
            }

            var result = host + ":" + port + ",abortConnect=false,connectTimeout=2000,syncTimeout=2000";
            var auth = Environment.GetEnvironmentVariable("REDIS_AUTH_STRING");
            if (!string.IsNullOrWhiteSpace(auth))
            {
                result += ",password=" + auth;
            }
            return result;
        }

        private static async Task<IDatabase> GetDatabaseAsync()
        {
            var connectionString = BuildConnectionString();
            if (connectionString == null)
            {
                return null;
            }

            try
            {
                Task<ConnectionMultiplexer> task;
                lock (ConnectLock)
                {
                    if (connectionTask == null || connectionTask.IsFaulted || connectionTask.IsCanceled)
                    {
                        connectionTask = ConnectionMultiplexer.ConnectAsync(connectionString);
                    }
                    task = connectionTask;
                }

                var multiplexer = await task.ConfigureAwait(false);
                return multiplexer.IsConnected ? multiplexer.GetDatabase() : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) where T : class
        {
            var redisKey = KeyPrefix + key;
            var db = await GetDatabaseAsync().ConfigureAwait(false);

            if (db != null)
            {
                try
                {
                    var cached = await db.StringGetAsync(redisKey).ConfigureAwait(false);
                    if (cached.HasValue)
                    {
                        return JsonConvert.DeserializeObject<T>((string)cached, SerializerSettings);
                    }
                }
                catch (Exception)
                {
                    // Cache failures must never break the request; fall through to the data source.
                }
            }

            var value = await factory().ConfigureAwait(false);

            if (db != null && value != null)
            {
                try
                {
                    var payload = JsonConvert.SerializeObject(value, SerializerSettings);
                    await db.StringSetAsync(redisKey, payload, ttl).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Ignore cache write failures.
                }
            }

            return value;
        }

        public async Task RemoveAsync(string key)
        {
            var db = await GetDatabaseAsync().ConfigureAwait(false);
            if (db == null)
            {
                return;
            }

            try
            {
                await db.KeyDeleteAsync(KeyPrefix + key).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Ignore cache invalidation failures; entries expire via TTL.
            }
        }
    }
}
