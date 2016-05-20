using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MongoDbCache
{
    public class MongoDbCache : IDistributedCache
    {
        #region private
        private DateTimeOffset _lastScan = DateTimeOffset.UtcNow;
        private TimeSpan _scanInterval;
        private readonly TimeSpan _defaultScanInterval = TimeSpan.FromMinutes(5);
        private readonly MongoContext _mongoContext;

        private void SetScanInterval(TimeSpan? scanInterval)
        {
            _scanInterval = scanInterval?.TotalSeconds > 0
                ? scanInterval.Value
                : _defaultScanInterval;
        }

        #endregion

        public MongoDbCache(IMongoDatabase mongoDatabase, IOptions<MongoDbCacheOptions> optionsAccessor)
        {
            var options = optionsAccessor.Value;
            _mongoContext = new MongoContext(mongoDatabase, options.CollectionName);
            SetScanInterval(options.ScanInterval);
        }

        public MongoDbCache(IOptions<MongoDbCacheOptions> optionsAccessor)
        {
            var options = optionsAccessor.Value;
            _mongoContext = new MongoContext(options.ConnectionString, options.DatabaseName, options.CollectionName);
            SetScanInterval(options.ScanInterval);
        }

        public byte[] Get(string key)
        {
            ScanAndDeleteExpired();

            return _mongoContext.GetCacheItem(key, withoutValue: false);
        }

        public async Task<byte[]> GetAsync(string key)
        {
            ScanAndDeleteExpired();

            return await _mongoContext.GetCacheItemAsync(key, withoutValue: false);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options = null)
        {
            ScanAndDeleteExpired();

            _mongoContext.Set(key, value, options);
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options = null)
        {
            ScanAndDeleteExpired();

            await _mongoContext.SetAsync(key, value, options);
        }

        public void Refresh(string key)
        {
            ScanAndDeleteExpired();

            _mongoContext.GetCacheItem(key, withoutValue: true);
        }

        public async Task RefreshAsync(string key)
        {
            ScanAndDeleteExpired();

            await _mongoContext.GetCacheItemAsync(key, withoutValue: true);
        }

        public void Remove(string key)
        {
            ScanAndDeleteExpired();

            _mongoContext.Remove(key);
        }

        public async Task RemoveAsync(string key)
        {
            ScanAndDeleteExpired();

            await _mongoContext.RemoveAsync(key);
        }

        private void ScanAndDeleteExpired()
        {
            if (_lastScan.Add(_scanInterval) < DateTimeOffset.UtcNow)
            {
                Task.Run(() =>
                {
                    _lastScan = DateTimeOffset.UtcNow;
                    _mongoContext.DeleteExpired();
                });
            }
        }
    }
}