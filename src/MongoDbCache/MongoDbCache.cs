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
            SetScanInterval(options.ExpiredScanInterval);
        }

        public MongoDbCache(IOptions<MongoDbCacheOptions> optionsAccessor)
        {
            var options = optionsAccessor.Value;
            _mongoContext = new MongoContext(options.ConnectionString, options.DatabaseName, options.CollectionName);
            SetScanInterval(options.ExpiredScanInterval);
        }

        public byte[] Get(string key)
        {
            var value = _mongoContext.GetCacheItem(key, withoutValue: false);

            ScanAndDeleteExpired();

            return value;
        }

        public async Task<byte[]> GetAsync(string key)
        {
            var value = await _mongoContext.GetCacheItemAsync(key, withoutValue: false);

            ScanAndDeleteExpired();

            return value;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options = null)
        {
            _mongoContext.Set(key, value, options);

            ScanAndDeleteExpired();
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options = null)
        {
            await _mongoContext.SetAsync(key, value, options);

            ScanAndDeleteExpired();
        }

        public void Refresh(string key)
        {
            _mongoContext.GetCacheItem(key, withoutValue: true);

            ScanAndDeleteExpired();
        }

        public async Task RefreshAsync(string key)
        {
            await _mongoContext.GetCacheItemAsync(key, withoutValue: true);

            ScanAndDeleteExpired();
        }

        public void Remove(string key)
        {
            _mongoContext.Remove(key);

            ScanAndDeleteExpired();
        }

        public async Task RemoveAsync(string key)
        {
            await _mongoContext.RemoveAsync(key);

            ScanAndDeleteExpired();
        }

        private void ScanAndDeleteExpired()
        {
            var utcNow = DateTimeOffset.UtcNow;

            if (_lastScan.Add(_scanInterval) < utcNow)
            {
                Task.Run(() =>
                {
                    _lastScan = utcNow;
                    _mongoContext.DeleteExpired(utcNow);
                });
            }
        }
    }
}