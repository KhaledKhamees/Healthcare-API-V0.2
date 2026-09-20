using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Healthcare_API_V0._2_Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace Healthcare_API_V0._2_Infrastructure.Caching
{
    public class CacheService : ICacheService
    {
        #region IDistributedCache implementation
        //private readonly IDistributedCache distributedCache;
        //public CacheService(IDistributedCache distributedCache)
        //{
        //    this.distributedCache = distributedCache;
        //}
        //public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        //{
        //    string cachedData = await distributedCache.GetStringAsync(key, cancellationToken);
        //    if (string.IsNullOrEmpty(cachedData))
        //    {
        //        return null;
        //    }
        //    T? value = System.Text.Json.JsonSerializer.Deserialize<T>(cachedData);
        //    return value;
        //}
        //public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        //{
        //    string serializedData = System.Text.Json.JsonSerializer.Serialize(value);
        //    await distributedCache.SetStringAsync(
        //        key, 
        //        serializedData, 
        //        new DistributedCacheEntryOptions
        //            {
        //                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
        //            }, 
        //        cancellationToken);
        //}

        //public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        //{
        //    await distributedCache.RemoveAsync(key, cancellationToken);
        //}

        #endregion

        #region Redis implementation 
        private readonly IConnectionMultiplexer redis;
        private readonly StackExchange.Redis.IDatabase database;
        public CacheService(IConnectionMultiplexer redis)
        {
            this.redis = redis;
            database = redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key
            , CancellationToken cancellationToken = default) 
            where T : class
        {
            RedisValue cachedData = await database.StringGetAsync(key);
            if (cachedData.IsNullOrEmpty)
            {
                return null;
            }
            return JsonSerializer.Deserialize<T>(cachedData);
        }

        public async Task SetAsync<T>(string key,
            T value,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            string serializedData = JsonSerializer.Serialize(value);
            await database.StringSetAsync(
                key,
                serializedData,
                expiration ?? TimeSpan.FromMinutes(30));
        }

        public async Task RemoveAsync(string key,
            CancellationToken cancellationToken = default)
        {
            await database.KeyDeleteAsync(key);
        }

        #endregion
    }
}
