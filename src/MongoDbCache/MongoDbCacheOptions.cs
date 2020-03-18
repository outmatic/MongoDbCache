using System;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MongoDbCache
{
    public class MongoDbCacheOptions : IOptions<MongoDbCacheOptions>
    {
        public string ConnectionString { get; set; }
        public MongoClientSettings MongoClientSettings { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
        public TimeSpan? ExpiredScanInterval { get; set; }

        MongoDbCacheOptions IOptions<MongoDbCacheOptions>.Value => this;
    }
}