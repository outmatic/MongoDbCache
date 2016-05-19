using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Driver;

namespace MongoDbCache
{
    internal class MongoContext
    {
        private readonly IMongoCollection<CacheItem> _collection;

        private static FilterDefinition<CacheItem> FilterByKey(string key)
        {
            return Builders<CacheItem>.Filter.Eq(x => x.Key, key);
        }

        private static FilterDefinition<CacheItem> FilterByExpiresAtNotNull()
        {
            return Builders<CacheItem>.Filter.Ne(x => x.ExpiresAt, null);
        }

        private static ProjectionDefinition<CacheItem> ProjectWithoutValue()
        {
            return Builders<CacheItem>.Projection.Exclude(x => x.Value);
        }

        private static bool CheckIfExpired(CacheItem cacheItem)
        {
            return cacheItem?.ExpiresAt <= DateTimeOffset.UtcNow;
        }

        private static DateTimeOffset? GetExpiresAt(double? slidingExpirationInSeconds, DateTimeOffset? absoluteExpiration)
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

        private CacheItem UpdateExpiresAtIfRequired(CacheItem cacheItem)
        {
            if (cacheItem.ExpiresAt == null)
            {
                return cacheItem;
            }

            var absoluteExpiration = GetExpiresAt(cacheItem.SlidingExpirationInSeconds, cacheItem.AbsoluteExpiration);
            _collection.UpdateOne(FilterByKey(cacheItem.Key) & FilterByExpiresAtNotNull(),
                Builders<CacheItem>.Update.Set(x => x.ExpiresAt, absoluteExpiration));

            return cacheItem.WithExpiresAt(absoluteExpiration);
        }

        private async Task<CacheItem> UpdateExpiresAtIfRequiredAsync(CacheItem cacheItem)
        {
            if (cacheItem.ExpiresAt == null)
            {
                return cacheItem;
            }

            var absoluteExpiration = GetExpiresAt(cacheItem.SlidingExpirationInSeconds, cacheItem.AbsoluteExpiration);
            await _collection.UpdateOneAsync(FilterByKey(cacheItem.Key) & FilterByExpiresAtNotNull(),
                Builders<CacheItem>.Update.Set(x => x.ExpiresAt, absoluteExpiration));

            return cacheItem.WithExpiresAt(absoluteExpiration);
        }

        public MongoContext(IMongoDatabase database, string collectionName)
        {
            _collection = database.GetCollection<CacheItem>(collectionName);
        }

        public MongoContext(string connectionString, string databaseName, string collectionName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _collection = database.GetCollection<CacheItem>(collectionName);
        }

        public void DeleteExpired()
        {
            _collection.DeleteMany(Builders<CacheItem>.Filter.Lte(x => x.ExpiresAt, DateTimeOffset.UtcNow));
        }

        public byte[] GetCacheItem(string key, bool withValue)
        {
            if (key == null)
            {
                return null;
            }

            var find = _collection.Find(FilterByKey(key));
            if (!withValue)
            {
                find = find.Project<CacheItem>(ProjectWithoutValue());
            }

            var cacheItem = find.SingleOrDefault();
            if (cacheItem == null)
            {
                return null;
            }

            if (CheckIfExpired(cacheItem))
            {
                Remove(cacheItem.Key);
                return null;
            }

            cacheItem = UpdateExpiresAtIfRequired(cacheItem);

            return cacheItem?.Value;
        }

        public async Task<byte[]> GetCacheItemAsync(string key, bool withValue)
        {
            if (key == null)
            {
                return null;
            }

            var find = _collection.Find(FilterByKey(key));
            if (!withValue)
            {
                find = find.Project<CacheItem>(ProjectWithoutValue());
            }

            var cacheItem = await find.SingleOrDefaultAsync();
            if (cacheItem == null)
            {
                return null;
            }

            if (CheckIfExpired(cacheItem))
            {
                Remove(cacheItem.Key);
                return null;
            }

            cacheItem = await UpdateExpiresAtIfRequiredAsync(cacheItem);

            return cacheItem?.Value;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options = null)
        {
            var absolutExpiration = options?.AbsoluteExpiration;
            var slidingExpirationInSeconds = options?.SlidingExpiration?.TotalSeconds;

            if (options?.AbsoluteExpirationRelativeToNow != null)
            {
                absolutExpiration = DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }

            if (absolutExpiration < DateTimeOffset.UtcNow)
            {
                //TODO throw exceptions
                return;
            }

            var expiresAt = GetExpiresAt(slidingExpirationInSeconds, absolutExpiration);
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
                absolutExpiration = DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }

            if (absolutExpiration < DateTimeOffset.UtcNow)
            {
                //TODO throw exceptions
                return;
            }

            var expiresAt = GetExpiresAt(slidingExpirationInSeconds, absolutExpiration);
            var cacheItem = new CacheItem(key, value, expiresAt, absolutExpiration, slidingExpirationInSeconds);

            await _collection.ReplaceOneAsync(FilterByKey(key), cacheItem, new UpdateOptions
            {
                IsUpsert = true
            });
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