using System;
using System.Threading;
using Microsoft.Extensions.Caching.Distributed;
using MongoDbCache.Tests.Infrastructure;
using Xunit;

namespace MongoDbCache.Tests
{
    public class TimeExpirationTests
    {
  
        [Fact]
        public void AbsoluteExpirationExpires()
        {
            var cache = MongoDbCacheConfig.CreateCacheInstance();
            const string key = "myKey1";
            var value = new byte[1];

            cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(1)));

            var result = cache.Get(key);

            Assert.Equal(value, result);

            for (var i = 0; i < 4 && (result != null); i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
                result = cache.Get(key);
            }

            Assert.Null(result);
        }


        [Fact]
        public void SlidingExpirationExpiresIfNotAccessed()
        {
            var cache = MongoDbCacheConfig.CreateCacheInstance();
            const string key = "myKey2";
            var value = new byte[1];

            cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(1)));

            var result = cache.Get(key);
            Assert.Equal(value, result);

            Thread.Sleep(TimeSpan.FromSeconds(3));

            result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void SlidingExpirationRenewedByAccess()
        {
            var cache = MongoDbCacheConfig.CreateCacheInstance();
            const string key = "myKey3";
            var value = new byte[1];

            cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(1)));

            var result = cache.Get(key);
            Assert.Equal(value, result);

            for (var i = 0; i < 5; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));

                result = cache.Get(key);
                Assert.Equal(value, result);
            }

            Thread.Sleep(TimeSpan.FromSeconds(3));
            result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void SlidingExpirationRenewedByAccessUntilAbsoluteExpiration()
        {
            var cache = MongoDbCacheConfig.CreateCacheInstance();
            const string key = "myKey4";
            var value = new byte[1];

            cache.Set(key, value, new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(1))
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(3)));

            var result = cache.Get(key);
            Assert.Equal(value, result);

            for (var i = 0; i < 5; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));

                result = cache.Get(key);
                Assert.Equal(value, result);
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));

            result = cache.Get(key);
            Assert.Null(result);
        }
    }
}
