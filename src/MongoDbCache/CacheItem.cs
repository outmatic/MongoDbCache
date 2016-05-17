using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbCache
{
    public class CacheItem
    {
        [BsonId]
        public string Key { get; private set; }
        public byte[] Value { get; private set; }
        public DateTimeOffset? ExpiresAt { get; private set; }
        public DateTimeOffset? AbsoluteExpiration { get; private set; }
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