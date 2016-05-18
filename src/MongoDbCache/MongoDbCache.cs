using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MongoDbCache
{
    public class MongoDbCache : IDistributedCache
    {
        #region private
        private readonly IMongoCollection<CacheItem> _collection;

        private static FilterDefinition<CacheItem> FilterByKey(string key)
        {
            return Builders<CacheItem>.Filter.Eq(x => x.Key, key);
        }

        private static FilterDefinition<CacheItem> FilterByExpiresAtNotNull()
        {
            return Builders<CacheItem>.Filter.Ne(x => x.ExpiresAt, null);
        }

        private static DateTimeOffset? GetExpiration(double? slidingExpirationInSeconds, DateTimeOffset? absoluteExpiration)
        {
            if (slidingExpirationInSeconds == null && absoluteExpiration == null)
            {
                return null;
            }

            if (slidingExpirationInSeconds == null)
            {
                return absoluteExpiration;
            }

            var seconds = slidingExpirationInSeconds.GetValueOrDefault();

            return DateTimeOffset.UtcNow.AddSeconds(seconds) > absoluteExpiration
                ? absoluteExpiration
                : DateTimeOffset.UtcNow.AddSeconds(seconds);
        }

        private CacheItem UpdateExpirationIfRequired(CacheItem cacheItem)
        {
            if (cacheItem.SlidingExpirationInSeconds == null && cacheItem.AbsoluteExpiration == null)
            {
                return cacheItem;
            }

            var absoluteExpiration = GetExpiration(cacheItem.SlidingExpirationInSeconds, cacheItem.AbsoluteExpiration);
            if (absoluteExpiration == null)
            {
                return cacheItem;
            }

            _collection.UpdateOne(FilterByKey(cacheItem.Key) & FilterByExpiresAtNotNull(),
                Builders<CacheItem>.Update.Set(x => x.ExpiresAt, absoluteExpiration));

            return cacheItem.SetExpiration(absoluteExpiration);
        }

        private async Task<CacheItem> UpdateExpirationIfRequiredAsync(CacheItem cacheItem)
        {
            if (cacheItem.SlidingExpirationInSeconds == null && cacheItem.AbsoluteExpiration == null)
            {
                return cacheItem;
            }

            var absoluteExpiration = GetExpiration(cacheItem.SlidingExpirationInSeconds, cacheItem.AbsoluteExpiration);
            if (absoluteExpiration == null)
            {
                return cacheItem;
            }

            await _collection.UpdateOneAsync(FilterByKey(cacheItem.Key) & FilterByExpiresAtNotNull(),
                Builders<CacheItem>.Update.Set(x => x.ExpiresAt, absoluteExpiration));

            return cacheItem.SetExpiration(absoluteExpiration);
        }

        private bool CheckIfExpired(CacheItem cacheItem)
        {
            if (cacheItem.ExpiresAt == null || cacheItem.ExpiresAt >= DateTimeOffset.UtcNow)
            {
                return false;
            }

            Remove(cacheItem.Key);

            return true;
        }

        private async Task<bool> CheckIfExpiredAsync(CacheItem cacheItem)
        {
            if (cacheItem.ExpiresAt == null || cacheItem.ExpiresAt >= DateTimeOffset.UtcNow)
            {
                return false;
            }

            await RemoveAsync(cacheItem.Key);

            return true;
        }
        #endregion

        public MongoDbCache(IMongoDatabase mongoDatabase, IOptions<MongoDbCacheOptions> optionsAccessor)
        {
            var options = optionsAccessor.Value;
            _collection = mongoDatabase.GetCollection<CacheItem>(options.CollectionName);
        }

        public MongoDbCache(IOptions<MongoDbCacheOptions> optionsAccessor)
        {
            var options = optionsAccessor.Value;
            var client = new MongoClient(options.ConnectionString);
            var database = client.GetDatabase(options.DatabaseName);
            _collection = database.GetCollection<CacheItem>(options.CollectionName);          
        }

        private byte[] GetItem(string key, bool withValue)
        {
            if (key == null)
            {
                return null;
            }

            var find = _collection.Find(FilterByKey(key));
            if (!withValue)
            {
                find = find.Project<CacheItem>(Builders<CacheItem>.Projection.Exclude(x => x.Value));
            }

            var cacheItem = find.SingleOrDefault();
            if (cacheItem == null)
            {
                return null;
            }

            if (CheckIfExpired(cacheItem))
            {
                return null;
            }

            cacheItem = UpdateExpirationIfRequired(cacheItem);

            return cacheItem?.Value;
        }

        private async Task<byte[]> GetItemAsync(string key, bool withValue)
        {
            if (key == null)
            {
                return null;
            }

            var find = _collection.Find(FilterByKey(key));
            if (!withValue)
            {
                find = find.Project<CacheItem>(Builders<CacheItem>.Projection.Exclude(x => x.Value));
            }

            var cacheItem = await find.SingleOrDefaultAsync();
            if (cacheItem == null)
            {
                return null;
            }

            if (await CheckIfExpiredAsync(cacheItem))
            {
                return null;
            }

            cacheItem = await UpdateExpirationIfRequiredAsync(cacheItem);

            return cacheItem?.Value;
        }

        public byte[] Get(string key)
        {
            return GetItem(key, true);
        }

        public async Task<byte[]> GetAsync(string key)
        {
            return await GetItemAsync(key, true);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options = null)
        {
            var absolutExpiration = options?.AbsoluteExpiration;
            var slidingExpirationInSeconds = options?.SlidingExpiration?.TotalSeconds;

            if (options?.AbsoluteExpirationRelativeToNow != null)
            {
                absolutExpiration = options.AbsoluteExpirationRelativeToNow.Value.TotalSeconds >= 1
                    ? DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value)
                    : DateTimeOffset.UtcNow.AddSeconds(-1);
            }

            if (absolutExpiration < DateTimeOffset.UtcNow || slidingExpirationInSeconds < 1)
            {
                return;
            }

            var expiresAt = GetExpiration(slidingExpirationInSeconds, absolutExpiration);

            var cacheItem = new CacheItem(key, value, expiresAt, absolutExpiration, slidingExpirationInSeconds);

            _collection.ReplaceOne(FilterByKey(key), cacheItem, new UpdateOptions
            {
                IsUpsert = true
            });
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options = null)
        {
            var absolutExpiration = options?.AbsoluteExpiration;
            var slidingExpirationInSeconds = options?.SlidingExpiration?.TotalSeconds;

            if (options?.AbsoluteExpirationRelativeToNow != null)
            {
                absolutExpiration = options.AbsoluteExpirationRelativeToNow.Value.TotalSeconds >= 1
                    ? DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value)
                    : DateTimeOffset.UtcNow.AddSeconds(-1);
            }

            if (absolutExpiration < DateTimeOffset.UtcNow || slidingExpirationInSeconds < 1)
            {
                return;
            }

            var expiresAt = GetExpiration(slidingExpirationInSeconds, absolutExpiration);

            var cacheItem = new CacheItem(key, value, expiresAt, absolutExpiration, slidingExpirationInSeconds);

            await _collection.ReplaceOneAsync(FilterByKey(key), cacheItem, new UpdateOptions
            {
                IsUpsert = true
            });
        }

        public void Refresh(string key)
        {
            GetItem(key, false);
        }

        public async Task RefreshAsync(string key)
        {
            await GetItemAsync(key, false);
        }

        public void Remove(string key)
        {
            _collection.DeleteOne(FilterByKey(key));
        }

        public async Task RemoveAsync(string key)
        {
            await _collection.DeleteOneAsync(FilterByKey(key));
        }
    }
}
