namespace SiloUseNetGenericHost.TypedOptions
{
    public class OrleansProviderOption
    {
        public string DefaultProvider { get; set; }

        public MongoDbProviderSettings MongoDB { get; set; }
    }

    public class MongoDbProviderSettings
    {
        public MongoDbProviderClusterSettings Cluster { get; set; }
        public MongoDbProviderStorageSettings Storage { get; set; }
        public MongoDbProviderReminderSettings Reminder { get; set; }
    }

    public class MongoDbProviderClusterSettings
    {
        public string DbConn { get; set; }
        public string DbName { get; set; }
        public string CollectionPrefix { get; set; }
    }

    public class MongoDbProviderStorageSettings
    {
        public string DbConn { get; set; }
        public string DbName { get; set; }
        public string CollectionPrefix { get; set; }
    }

    public class MongoDbProviderReminderSettings
    {
        public string DbConn { get; set; }
        public string DbName { get; set; }
        public string CollectionPrefix { get; set; }
    }
}