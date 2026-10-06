using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    /// <summary>Cache-aside over Redis with an IMemoryCache fallback.</summary>
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string prefix, string key, CancellationToken cancellationToken = default);

        Task SetAsync<T>(string prefix, string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);

        Task RemoveAsync(string prefix, string key, CancellationToken cancellationToken = default);
    }

    public sealed class CacheService : ICacheService
    {
        private static readonly string[] AllowedPrefixes = { "blacklist:", "attempts:", "lockout:", "cache:" };
        private static readonly string[] ForbiddenPatterns = { "password", "secret", "token" };

        private readonly IConnectionMultiplexer _redis;
        private readonly IMemoryCache _memory;

        public CacheService(IConnectionMultiplexer redis, IMemoryCache memory)
        {
            ArgumentNullException.ThrowIfNull(redis);
            ArgumentNullException.ThrowIfNull(memory);
            _redis = redis;
            _memory = memory;
        }

        private static string BuildKey(string prefix, string key)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("Prefix is required.", nameof(prefix));
            }

            if (Array.IndexOf(AllowedPrefixes, prefix) < 0)
            {
                throw new ArgumentException($"Prefix '{prefix}' is not allowed.", nameof(prefix));
            }

            var uppered = (prefix + key).ToUpperInvariant();
            if (ForbiddenPatterns.Any(forbidden => uppered.Contains(forbidden.ToUpperInvariant(), StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Forbidden cache key pattern.");
            }

            return prefix + key;
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync<T>(string prefix, string key, CancellationToken cancellationToken = default)
        {
            var cacheKey = BuildKey(prefix, key);
            try
            {
                if (_redis.IsConnected)
                {
                    var db = _redis.GetDatabase();
                    var cached = await db.StringGetAsync(cacheKey).ConfigureAwait(false);
                    if (cached.HasValue)
                    {
                        return JsonSerializer.Deserialize<T>((string)cached!);
                    }
                }
            }
            catch (RedisException ex)
            {
                _ = ex;
            }
            catch (InvalidOperationException ex)
            {
                _ = ex;
            }

            if (_memory.TryGetValue(cacheKey, out T? value))
            {
                return value;
            }

            return default;
        }

        /// <inheritdoc />
        public async Task SetAsync<T>(string prefix, string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            var cacheKey = BuildKey(prefix, key);
            if (ttl <= TimeSpan.Zero || ttl > TimeSpan.FromMinutes(2))
            {
                throw new ArgumentOutOfRangeException(nameof(ttl), "TTL must be between 0 and 120 seconds.");
            }

            try
            {
                if (_redis.IsConnected)
                {
                    var db = _redis.GetDatabase();
                    await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(value), ttl).ConfigureAwait(false);
                    _memory.Set(cacheKey, value, ttl);
                    return;
                }
            }
            catch (RedisException ex)
            {
                _ = ex;
            }

            _memory.Set(cacheKey, value, ttl);
        }

        /// <inheritdoc />
        public async Task RemoveAsync(string prefix, string key, CancellationToken cancellationToken = default)
        {
            var cacheKey = BuildKey(prefix, key);
            try
            {
                if (_redis.IsConnected)
                {
                    var db = _redis.GetDatabase();
                    await db.KeyDeleteAsync(cacheKey).ConfigureAwait(false);
                    return;
                }
            }
            catch (RedisException ex)
            {
                _ = ex;
            }

            _memory.Remove(cacheKey);
        }
    }
}
