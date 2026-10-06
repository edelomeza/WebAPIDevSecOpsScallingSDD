using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using WebAPIDevSecOpsScallingSDD.Services;

namespace SecurityTest.Cache
{
    public class LockoutTests
    {
        private static CacheService CreateCache(IConnectionMultiplexer redis, MemoryCache memory)
        {
            return new CacheService(redis, memory);
        }

        [Fact]
        public async Task SetGetLockoutWorksInFallbackMode()
        {
            using var memory = new MemoryCache(new MemoryCacheOptions());
            var redis = await ConnectionMultiplexer.ConnectAsync(ConfigurationOptions.Parse("localhost:6390,abortConnect=false,connectTimeout=250"));
            var cache = CreateCache(redis, memory);
            try
            {
                await cache.SetAsync("lockout:", "user1", "locked", TimeSpan.FromMinutes(1));
                var value = await cache.GetAsync<string>("lockout:", "user1");
                Assert.Equal("locked", value);
                await cache.RemoveAsync("lockout:", "user1");
                Assert.Null(await cache.GetAsync<string>("lockout:", "user1"));
            }
            finally
            {
                await redis.DisposeAsync();
            }
        }

        [Fact]
        public async Task CacheKeyWithPasswordIsRejected()
        {
            using var memory = new MemoryCache(new MemoryCacheOptions());
            var redis = await ConnectionMultiplexer.ConnectAsync(ConfigurationOptions.Parse("localhost:6390,abortConnect=false,connectTimeout=250"));
            var cache = CreateCache(redis, memory);
            try
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => cache.SetAsync("cache:", "user:password", "x", TimeSpan.FromMinutes(1)));
            }
            finally
            {
                await redis.DisposeAsync();
            }
        }

        [Fact]
        public async Task UnknownPrefixIsRejected()
        {
            using var memory = new MemoryCache(new MemoryCacheOptions());
            var redis = await ConnectionMultiplexer.ConnectAsync(ConfigurationOptions.Parse("localhost:6390,abortConnect=false,connectTimeout=250"));
            var cache = CreateCache(redis, memory);
            try
            {
                await Assert.ThrowsAsync<ArgumentException>(() => cache.SetAsync("random:", "k", "v", TimeSpan.FromMinutes(1)));
            }
            finally
            {
                await redis.DisposeAsync();
            }
        }

        [Fact]
        public async Task TtlOutOfRangeIsRejected()
        {
            using var memory = new MemoryCache(new MemoryCacheOptions());
            var redis = await ConnectionMultiplexer.ConnectAsync(ConfigurationOptions.Parse("localhost:6390,abortConnect=false,connectTimeout=250"));
            var cache = CreateCache(redis, memory);
            try
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => cache.SetAsync("cache:", "k", "v", TimeSpan.FromMinutes(5)));
            }
            finally
            {
                await redis.DisposeAsync();
            }
        }
    }
}
