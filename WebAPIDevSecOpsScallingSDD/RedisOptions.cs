namespace WebAPIDevSecOpsScallingSDD
{
    /// <summary>Configuration binding for the Redis cache section.</summary>
    internal sealed class RedisOptions
    {
        /// <summary>Gets or sets the Redis connection string.</summary>
        public string ConnectionString { get; set; } = "localhost:6379,abortConnect=false";
    }
}
