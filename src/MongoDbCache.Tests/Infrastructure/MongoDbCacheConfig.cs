using Microsoft.Extensions.Caching.Distributed;
using System;

namespace MongoDbCache.Tests.Infrastructure
{
    public static class MongoDbCacheConfig
    {
        public static IDistributedCache CreateCacheInstance()
        {
            return new MongoDbCache(CreateOptions());
        }

        public static MongoDbCacheOptions CreateOptions()
        {
            return new MongoDbCacheOptions
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "MongoCache",
                CollectionName = "appcache",
                ExpiredScanInterval = TimeSpan.FromMinutes(10)
            };
        }
    }
}
