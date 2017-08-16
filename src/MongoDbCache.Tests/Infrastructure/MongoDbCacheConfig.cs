using Microsoft.Extensions.Caching.Distributed;

namespace MongoDbCache.Tests.Infrastructure
{
    public class MongoDbCacheConfig
    {
        public static IDistributedCache CreateCacheInstance()
        {
            return new MongoDbCache(new MongoDbCacheOptions
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "CacheTests",
                CollectionName = "caches"
            });
        }
    }
}
