using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.Redis;
using WebAPIDevSecOpsScallingSDD.Services;

namespace IntegrationTest.Cache
{
    public class CacheFallbackTests : IAsyncLifetime
    {
        private readonly RedisContainer _redis = new RedisBuilder("redis:8.0").Build();

        public async Task InitializeAsync() => await _redis.StartAsync().ConfigureAwait(false);

        public async Task DisposeAsync() => await _redis.DisposeAsync().ConfigureAwait(false);

        [Fact]
        public async Task CacheSetGetWorksAgainstRealRedis()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            await WaitForReady(client, System.Net.HttpStatusCode.OK);

            var cache = factory.Services.GetRequiredService<ICacheService>();
            await cache.SetAsync("cache:", "item:42", "hello", TimeSpan.FromSeconds(30));
            Assert.Equal("hello", await cache.GetAsync<string>("cache:", "item:42"));
        }

        [Fact]
        public async Task FallbackToMemoryWhenRedisGoesDown()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            await WaitForReady(client, System.Net.HttpStatusCode.OK);

            var cache = factory.Services.GetRequiredService<ICacheService>();
            await cache.SetAsync("cache:", "item:99", "valor", TimeSpan.FromSeconds(30));

            await _redis.StopAsync();

            var stopwatch = Stopwatch.StartNew();
            var value = await cache.GetAsync<string>("cache:", "item:99");
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds <= 500, $"Fallback took {stopwatch.ElapsedMilliseconds} ms");
            Assert.Equal("valor", value);

            await WaitForReady(client, System.Net.HttpStatusCode.ServiceUnavailable);

            var liveAfter = await client.GetAsync(new Uri("/health", UriKind.Relative));
            Assert.Equal(System.Net.HttpStatusCode.OK, liveAfter.StatusCode);
        }

        private static async Task WaitForReady(HttpClient client, System.Net.HttpStatusCode expected)
        {
            for (var i = 0; i < 20; i++)
            {
                var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative)).ConfigureAwait(false);
                if (response.StatusCode == expected)
                {
                    return;
                }

                await Task.Delay(500).ConfigureAwait(false);
            }

            Assert.Fail($"Expected /health/ready to become {expected}.");
        }

        private WebApplicationFactory<Program> CreateFactory()
        {
#pragma warning disable CA2000 // The instance is returned to the caller, which disposes it.
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
#pragma warning restore CA2000
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?> { ["Redis:ConnectionString"] = _redis.GetConnectionString() });
                });
            });
        }
    }
}
