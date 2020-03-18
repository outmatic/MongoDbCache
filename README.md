# MongoDbCache
A distributed cache implementation based on MongoDb, inspired by RedisCache and SqlServerCache (see https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed).

### How do I get started?

Install the nuget package

    PM> Install-Package MongoDbCache

You can either choose to use the provided extension method or register the implementation in the ConfigureServices method.
The mongo connection settings can be passed as either a connection string or MongoClientSettings object.

```csharp
public void ConfigureServices(IServiceCollection services)
{  
	services.AddMongoDbCache(options => {
		options.ConnectionString = "mongodb://localhost:27017",
		options.DatabaseName = "MongoCache",
		options.CollectionName = "appcache",
		options.ExpiredScanInterval = TimeSpan.FromMinutes(10)
	});
}
```
```csharp
public void ConfigureServices(IServiceCollection services)
{  
    var mongoSettings = new MongoClientSettings();

	services.AddMongoDbCache(options => {
		options.MongoClientSettings = mongoSettings,
		options.DatabaseName = "MongoCache",
		options.CollectionName = "appcache",
		options.ExpiredScanInterval = TimeSpan.FromMinutes(10)
	});
}
```

MongoDbCache implements IDistributedCache, therefore you can use all the sync and async methods provided by the interface, please see https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed.
