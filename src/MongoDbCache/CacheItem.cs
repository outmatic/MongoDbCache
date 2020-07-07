using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbCache
{
    internal class CacheItem
    {
        [BsonId]
        public string Key { get; }

        [BsonElement("v")]
        public byte[] Value { get; }

        [BsonElement("e")]
        public DateTimeOffset? ExpiresAt { get; private set; }

        [BsonElement("a")]
        public DateTimeOffset? AbsoluteExpiration { get; }

        [BsonElement("s")]
        public double? SlidingExpirationInSeconds { get; }

        [BsonConstructor]
        public CacheItem(string key, byte[] value, DateTimeOffset? expiresAt, DateTimeOffset? absoluteExpiration, double? slidingExpirationInSeconds)
        {
            Key = key;
            Value = value;
            ExpiresAt = expiresAt;
            AbsoluteExpiration = absoluteExpiration;
            SlidingExpirationInSeconds = slidingExpirationInSeconds;
        }

        [BsonConstructor]
        public CacheItem(string key, DateTimeOffset? expiresAt, DateTimeOffset? absoluteExpiration, double? slidingExpirationInSeconds)
            : this(key, null, expiresAt, absoluteExpiration, slidingExpirationInSeconds)
        {

        }

        public CacheItem WithExpiresAt(DateTimeOffset? expiresAt)
        {
            ExpiresAt = expiresAt;
            return this;
        }
    }
}