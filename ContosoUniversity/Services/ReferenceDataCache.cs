using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ContosoUniversity.Services
{
    /// <summary>Serializable lookup entry used for dropdown reference data.</summary>
    public class LookupItem
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// Non-blocking distributed cache for reference data backed by GCP Memorystore (Redis).
    /// Configure with REDIS_CONNECTION_STRING or REDIS_HOST (+ optional REDIS_PORT).
    /// Fails open: if Redis is not configured or unavailable the data source is queried directly.
    /// </summary>
    public static class ReferenceDataCache
    {
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
        private static readonly Lazy<ConnectionMultiplexer> Connection = new Lazy<ConnectionMultiplexer>(Connect);

        private static ConnectionMultiplexer Connect()
        {
            var cs = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
            if (string.IsNullOrEmpty(cs))
            {
                var host = Environment.GetEnvironmentVariable("REDIS_HOST");
                if (string.IsNullOrEmpty(host))
                {
                    return null;
                }
                var port = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
                cs = host + ":" + port;
            }
            var options = ConfigurationOptions.Parse(cs);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 2000;
            return ConnectionMultiplexer.Connect(options);
        }

        public static async Task<List<LookupItem>> GetOrAddAsync(string key, Func<Task<List<LookupItem>>> factory)
        {
            IDatabase redis = null;
            try
            {
                var conn = Connection.Value;
                if (conn != null)
                {
                    redis = conn.GetDatabase();
                    var cached = await redis.StringGetAsync("contoso:" + key).ConfigureAwait(false);
                    if (cached.HasValue)
                    {
                        return JsonConvert.DeserializeObject<List<LookupItem>>((string)cached);
                    }
                }
            }
            catch (Exception)
            {
                redis = null; // fail open
            }

            var items = await factory().ConfigureAwait(false);

            if (redis != null)
            {
                try
                {
                    await redis.StringSetAsync("contoso:" + key, JsonConvert.SerializeObject(items), DefaultTtl).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // cache write failures must not affect the request
                }
            }
            return items;
        }

        public static async Task<SelectList> GetSelectListAsync(string key, Func<Task<List<LookupItem>>> factory, object selectedValue = null)
        {
            var items = await GetOrAddAsync(key, factory).ConfigureAwait(false);
            return new SelectList(items, "Value", "Text", selectedValue == null ? null : selectedValue.ToString());
        }
    }
}
