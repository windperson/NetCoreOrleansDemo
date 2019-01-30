namespace WebClient.TypedOptions
{
    public class OrleansProviderOption
    {
        public string DefaultProvider { get; set; }

        public MongoDbProviderSettings MongoDB { get; set; }
    }

    public class MongoDbProviderSettings
    {
        public MongoDbProviderClusterSettings Cluster { get; set; }
    }

    public class MongoDbProviderClusterSettings
    {
        public string DbConn { get; set; }
        public string DbName { get; set; }
        public string CollectionPrefix { get; set; }
    }
}