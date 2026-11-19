using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Caching
{
    /// <summary>
    /// Non-blocking distributed cache abstraction used by controllers so that
    /// view data is fully materialized asynchronously before Razor rendering starts.
    /// </summary>
    public interface IAsyncCache
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) where T : class;

        Task RemoveAsync(string key);
    }
}
