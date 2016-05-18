using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MongoDbCache
{
    public static class MongoDbCacheServicesExtensions
    {
        public static IServiceCollection AddMongoDbCache(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.TryAdd(ServiceDescriptor.Singleton<IDistributedCache, MongoDbCache>());
            return services;
        }
    }
}
