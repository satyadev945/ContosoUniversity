using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ContosoUniversity.Caching
{
    /// <summary>
    /// Non-blocking read-through cache backed by AWS ElastiCache (Redis).
    /// The endpoint is supplied through the ELASTICACHE_ENDPOINT environment variable
    /// (e.g. "my-cluster.xxxxxx.use1.cache.amazonaws.com:6379"). When the variable is not set,
    /// or the cache is unreachable, data is loaded directly from the database so the
    /// application keeps working.
    /// </summary>
    public static class ReferenceDataCache
    {
        private static readonly Lazy<Task<ConnectionMultiplexer>> LazyConnection =
            new Lazy<Task<ConnectionMultiplexer>>(ConnectAsync);

        private static async Task<ConnectionMultiplexer> ConnectAsync()
        {
            var endpoint = Environment.GetEnvironmentVariable("ELASTICACHE_ENDPOINT");
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return null;
            }

            var options = ConfigurationOptions.Parse(endpoint);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 2000;
            options.AsyncTimeout = 2000;

            var useTls = Environment.GetEnvironmentVariable("ELASTICACHE_USE_TLS");
            if (string.Equals(useTls, "true", StringComparison.OrdinalIgnoreCase))
            {
                options.Ssl = true;
            }

            var password = Environment.GetEnvironmentVariable("ELASTICACHE_AUTH_TOKEN");
            if (!string.IsNullOrEmpty(password))
            {
                options.Password = password;
            }

            return await ConnectionMultiplexer.ConnectAsync(options).ConfigureAwait(false);
        }

        private static TimeSpan Ttl
        {
            get
            {
                int seconds;
                return int.TryParse(Environment.GetEnvironmentVariable("ELASTICACHE_TTL_SECONDS"), out seconds) && seconds > 0
                    ? TimeSpan.FromSeconds(seconds)
                    : TimeSpan.FromSeconds(60);
            }
        }

        /// <summary>Returns the cached value for <paramref name="key"/> or loads it with <paramref name="factory"/> and caches it.</summary>
        public static async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
        {
            IDatabase cache = null;
            try
            {
                var connection = await LazyConnection.Value.ConfigureAwait(false);
                if (connection != null && connection.IsConnected)
                {
                    cache = connection.GetDatabase();
                    var cached = await cache.StringGetAsync(key).ConfigureAwait(false);
                    if (cached.HasValue)
                    {
                        return JsonConvert.DeserializeObject<T>(cached.ToString());
                    }
                }
            }
            catch (Exception)
            {
                // Cache is an optimisation only - fall back to the database.
                cache = null;
            }

            var value = await factory().ConfigureAwait(false);

            if (cache != null)
            {
                try
                {
                    await cache.StringSetAsync(key, JsonConvert.SerializeObject(value), Ttl).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Ignore cache write failures.
                }
            }

            return value;
        }

        /// <summary>Removes a cached entry so that the next read reloads it from the database.</summary>
        public static async Task InvalidateAsync(string key)
        {
            try
            {
                var connection = await LazyConnection.Value.ConfigureAwait(false);
                if (connection != null && connection.IsConnected)
                {
                    await connection.GetDatabase().KeyDeleteAsync(key).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                // Ignore cache failures.
            }
        }
    }

    /// <summary>Lightweight id/text pair used to cache drop-down reference data.</summary>
    public class LookupItem
    {
        public int Key { get; set; }
        public string Text { get; set; }
    }

    public static class CacheKeys
    {
        public const string Departments = "contoso:lookup:departments";
        public const string OfficeAssignments = "contoso:lookup:officeassignments";
        public const string EnrollmentStatistics = "contoso:stats:enrollment";
    }
}
