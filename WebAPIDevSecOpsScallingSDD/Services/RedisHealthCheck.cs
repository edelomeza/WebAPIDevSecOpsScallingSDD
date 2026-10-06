using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    /// <summary>Reports whether the Redis cache store is reachable.</summary>
    internal sealed class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisHealthCheck(IConnectionMultiplexer redis)
        {
            ArgumentNullException.ThrowIfNull(redis);
            _redis = redis;
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    return HealthCheckResult.Unhealthy("Redis is not connected.");
                }

                var db = _redis.GetDatabase();
                await db.PingAsync().ConfigureAwait(false);
                return HealthCheckResult.Healthy("Redis reachable.");
            }
            catch (Exception ex) when (ex is RedisException or InvalidOperationException or System.IO.IOException)
            {
                return HealthCheckResult.Unhealthy($"Redis unreachable: {ex.GetType().Name}");
            }
        }
    }
}
