using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace MongoDbCache.Tests
{
    internal class TestDistributedCache : IDistributedCache
    {
        public void Connect()
            => throw new NotImplementedException();

        public Task ConnectAsync()
            => throw new NotImplementedException();

        public byte[] Get(string key)
            => throw new NotImplementedException();

        public Task<byte[]> GetAsync(string key, CancellationToken token = default)
            => throw new NotImplementedException();

        public void Refresh(string key)
            => throw new NotImplementedException();

        public Task RefreshAsync(string key, CancellationToken token = default)
            => throw new NotImplementedException();

        public void Remove(string key)
            => throw new NotImplementedException();

        public Task RemoveAsync(string key, CancellationToken token = default)
            => throw new NotImplementedException();

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            => throw new NotImplementedException();

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
            => throw new NotImplementedException();

        public bool TryGetValue(string key, out Stream value)
            => throw new NotImplementedException();
    }
}
