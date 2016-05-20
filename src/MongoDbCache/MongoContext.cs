using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDbCache
{
    internal class MongoContext
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

        private IFindFluent<CacheItem, CacheItem> GetItemQuery(string key, bool withoutValue)
        {
            var query = _collection.Find(FilterByKey(key));
            if (withoutValue)
            {
                query = query.Project<CacheItem>(Builders<CacheItem>.Projection.Exclude(x => x.Value));
            }

            return query;
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
        #endregion

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

        public byte[] GetCacheItem(string key, bool withoutValue)
        {
            if (key == null)
            {
                return null;
            }

            var query = GetItemQuery(key, withoutValue);
            var cacheItem = query.SingleOrDefault();
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

        public async Task<byte[]> GetCacheItemAsync(string key, bool withoutValue)
        {
            if (key == null)
            {
                return null;
            }

            var query = GetItemQuery(key, withoutValue);
            var cacheItem = await query.SingleOrDefaultAsync();
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
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options?.AbsoluteExpirationRelativeToNow?.Ticks < 0)
            {

            }

            var absolutExpiration = options?.AbsoluteExpiration;
            var slidingExpirationInSeconds = options?.SlidingExpiration?.TotalSeconds;

            if (options?.AbsoluteExpirationRelativeToNow != null)
            {
                absolutExpiration = DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }

            if (absolutExpiration <= DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException("The absolute expiration value must be in the future.");
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
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

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