namespace MongoDbCache
{
    public class MongoDbCacheOptions
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
    }
}