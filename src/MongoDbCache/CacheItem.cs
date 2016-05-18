using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbCache
{
    internal class CacheItem
    {
        [BsonId]
        public string Key { get; private set; }
        [BsonElement("value")]
        public byte[] Value { get; private set; }
        [BsonElement("expiresAt")]
        public DateTimeOffset? ExpiresAt { get; private set; }
        [BsonElement("absoluteExpiration")]
        public DateTimeOffset? AbsoluteExpiration { get; private set; }
        [BsonElement("slidingExpirationInSeconds")]
        public double? SlidingExpirationInSeconds { get; private set; }

        public CacheItem(string key, byte[] value, DateTimeOffset? expiresAt, DateTimeOffset? absoluteExpiration, double? slidingExpirationInSeconds)
        {
            Key = key;
            Value = value;
            ExpiresAt = expiresAt;
            AbsoluteExpiration = absoluteExpiration;
            SlidingExpirationInSeconds = slidingExpirationInSeconds;
        }

        public CacheItem SetExpiration(DateTimeOffset? expiresAt)
        {
            return new CacheItem(Key, Value, expiresAt, AbsoluteExpiration, SlidingExpirationInSeconds);
        }
    }
}