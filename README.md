# MongoDbCache
An distributed cache implementation based on MongoDb, inspired by RedisCache and SqlServerCache (see https://docs.asp.net/en/latest/performance/caching/distributed.html).

### How do I get started?

Install the nuget package

    PM> Install-Package MongoDbCache -Pre

You can either choose to use the provided extension method or register the implementation in the ConfigureServices:

```csharp
public void ConfigureServices(IServiceCollection services)
{
  // add via the extension method
  services.AddMongoDbCache();
  
  // or manually register the implementation
  services.AddSingleton<IDistributedCache>(serviceProvider =>
    new MongoDbCache.MongoDbCache(new MongoDbCacheOptions
    {
      ConnectionString = "mongodb://localhost",
      CollectionName = "cache",
      DatabaseName = "CacheDb",
      ScanInterval = TimeSpan.FromMinutes(5)
    }));
}
```
