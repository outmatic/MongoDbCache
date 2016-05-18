using Microsoft.Extensions.Caching.Distributed;

namespace MongoDbCache.Tests.Infrastructure
{
    public class MongoDbCacheConfig
    {
        public static IDistributedCache CreateCacheInstance()
        {
            return new MongoDbCache(new MongoDbCacheOptions
            {
                ConnectionString = "mongodb://localhost",
                DatabaseName = "CacheTests",
                CollectionName = "caches"
            });
        }
    }
}
