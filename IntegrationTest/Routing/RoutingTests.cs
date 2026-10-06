using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationTest.Routing
{
    public class RoutingTests
    {
        [Fact]
        public async Task VersionedRouteResolvesWithPascalCaseJson()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/api/v1/ping", UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"Status\":\"Pong\"", body, StringComparison.Ordinal);
            Assert.DoesNotContain("\"status\":\"pong\"", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task UnversionedRouteDoesNotResolve()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/api/ping", UriKind.Relative));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task OpenApiAndScalarAvailableInDevelopment()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var openApi = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
            var scalar = await client.GetAsync(new Uri("/scalar/v1", UriKind.Relative));

            Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);
            Assert.Equal(HttpStatusCode.OK, scalar.StatusCode);
        }

        [Fact]
        public async Task OpenApiAndScalarHiddenInProduction()
        {
#pragma warning disable CA2000 // The instance is returned to the caller, which disposes it.
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
#pragma warning restore CA2000
            using var client = factory.CreateClient();

            var openApi = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
            var scalar = await client.GetAsync(new Uri("/scalar/v1", UriKind.Relative));

            Assert.Equal(HttpStatusCode.NotFound, openApi.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, scalar.StatusCode);
        }
    }
}
