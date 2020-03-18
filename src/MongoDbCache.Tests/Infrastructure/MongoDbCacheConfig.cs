using Microsoft.Extensions.Caching.Distributed;
using System;
using MongoDB.Driver;

namespace MongoDbCache.Tests.Infrastructure
{
    public static class MongoDbCacheConfig
    {
        public static IDistributedCache CreateCacheInstance()
        {
            var useMongoClientSettings =
                Environment.GetEnvironmentVariable("MongoDbCacheTestsUseMongoClientSettings") == "true";
            
            return new MongoDbCache(useMongoClientSettings ? CreateOptionsWithMongoClientSettings() : CreateOptions());
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

        private static MongoDbCacheOptions CreateOptionsWithMongoClientSettings()
        {
            return new MongoDbCacheOptions
            {
                MongoClientSettings = new MongoClientSettings
                {
                    Server = MongoServerAddress.Parse("localhost")
                },
                DatabaseName = "MongoCache",
                CollectionName = "appcache",
                ExpiredScanInterval = TimeSpan.FromMinutes(10)
            };
        }
    }
}
