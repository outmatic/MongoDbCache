using Microsoft.Extensions.Caching.Distributed;
using MongoDbCache.Tests.Infrastructure;
using Xunit;

namespace MongoDbCache.Tests
{
    public class MongoDbCacheSetAndRemoveTests
    {
        [Fact]
        public void GetMissingKeyReturnsNull()
        {
            var cache = MongoDbCacheConfig.CreateCacheInstance();
            const string key = "non-existent-key";

            var result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void SetAndGetReturnsObject()
        {
            var cache = MongoDbCacheConfig.CreateCacheInstance();
            
            var value = new byte[1];
            const string key = "myKey";

            cache.Set(key, value);

            var result = cache.Get(key);
            Assert.Equal(value, result);
        }

        [Fact]
        public void SetAndGetWorksWithCaseSensitiveKeys()
        {
            var cache = MongoDbCacheConfig.CreateCacheInstance();
            var value = new byte[1];
            const string key1 = "myKey";
            const string key2 = "Mykey";

            cache.Set(key1, value);

            var result = cache.Get(key1);
            Assert.Equal(value, result);

            result = cache.Get(key2);
            Assert.Null(result);
        }

        [Fact]
        public void SetAlwaysOverwrites()
        {
            var cache = MongoDbCacheConfig.CreateCacheInstance();
            var value1 = new byte[] { 1 };
            const string key = "myKey";

            cache.Set(key, value1);
            var result = cache.Get(key);
            Assert.Equal(value1, result);

            var value2 = new byte[] { 2 };
            cache.Set(key, value2);
            result = cache.Get(key);
            Assert.Equal(value2, result);
        }

        [Fact]
        public void RemoveRemoves()
        {
            var cache = MongoDbCacheConfig.CreateCacheInstance();
            var value = new byte[1];
            const string key = "myKey";

            cache.Set(key, value);
            var result = cache.Get(key);
            Assert.Equal(value, result);

            cache.Remove(key);
            result = cache.Get(key);
            Assert.Null(result);
        }
    }
}
