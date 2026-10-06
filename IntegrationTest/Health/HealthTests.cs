using System;
using System.Net;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace IntegrationTest.Health
{
    public class HealthTests
    {
        private static WebApplicationFactory<Program> CreateFactory()
        {
#pragma warning disable CA2000
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?> { ["Redis:ConnectionString"] = "localhost:6390,abortConnect=false,connectTimeout=250" });
                });
            });
#pragma warning restore CA2000
        }

        [Fact]
        public async Task HealthReturns200()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task HealthReadyReturns503WhenRedisIsDown()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
    }
}
