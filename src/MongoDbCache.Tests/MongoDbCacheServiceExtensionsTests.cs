using System.Linq;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using MongoDbCache.Tests.Infrastructure;
using Xunit;

namespace MongoDbCache.Tests
{
    public class MongoDbCacheServiceExtensionsTests
    {
        [Fact]
        public void AddMongoDbCache_RegistersDistributedCacheAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMongoDbCache(options => {
                options = MongoDbCacheConfig.CreateOptions();
            });

            // Assert
            var distributedCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));

            Assert.NotNull(distributedCache);
            Assert.Equal(ServiceLifetime.Singleton, distributedCache.Lifetime);
        }

        [Fact]
        public void AddMongoDbCache_ReplaceUserRegisteredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IDistributedCache, TestDistributedCache>();

            var defaultOptions = MongoDbCacheConfig.CreateOptions();

            // Act
            services.AddMongoDbCache(options => {
                options.CollectionName = defaultOptions.CollectionName;
                options.ConnectionString = defaultOptions.ConnectionString;
                options.DatabaseName = defaultOptions.DatabaseName;
                options.ExpiredScanInterval = defaultOptions.ExpiredScanInterval;
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            var distributedCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));

            Assert.NotNull(distributedCache);
            Assert.Equal(ServiceLifetime.Singleton, distributedCache.Lifetime);
            Assert.IsType<MongoDbCache>(serviceProvider.GetRequiredService<IDistributedCache>());
        }
    }
}
