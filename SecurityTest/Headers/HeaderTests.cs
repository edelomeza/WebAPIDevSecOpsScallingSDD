using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SecurityTest.Headers
{
    public class HeaderTests
    {
        [Fact]
        public async Task AnonymousHealthEmitsSecurityHeaders()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AssertStaticHeaders(response);
            AssertCspWithNonce(response);
        }

        [Fact]
        public async Task ForbiddenProbeEmitsSecurityHeaders()
        {
            using var factory = CreateProbeFactory("Staging");
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/api/v1/probe/forbidden", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            AssertStaticHeaders(response);
            AssertCspWithNonce(response);
        }

        [Fact]
        public void ProductionHstsMaxAgeIs365DaysWithoutSubDomains()
        {
            // NOTE (04-03): TestServer siempre sirve http, asi que UseHsts nunca
            // emite el header en wire (requiere IsHttps). Nuestro delta es el
            // MaxAge via AddHsts; la emision es codigo del framework.
            using var factory = CreateProbeFactory("Production");
            var options = factory.Services.GetRequiredService<IOptions<HstsOptions>>().Value;

            Assert.Equal(TimeSpan.FromDays(365), options.MaxAge);
            Assert.False(options.IncludeSubDomains);
            Assert.False(options.Preload);
        }

        private static void AssertStaticHeaders(System.Net.Http.HttpResponseMessage response)
        {
            Assert.Equal("nosniff", GetHeader(response, "X-Content-Type-Options"));
            Assert.Equal("DENY", GetHeader(response, "X-Frame-Options"));
            Assert.False(string.IsNullOrWhiteSpace(GetHeader(response, "Referrer-Policy")));
            Assert.Equal("strict-origin-when-cross-origin", GetHeader(response, "Referrer-Policy"));
            Assert.Equal("0", GetHeader(response, "X-XSS-Protection"));
        }

        private static void AssertCspWithNonce(System.Net.Http.HttpResponseMessage response)
        {
            var csp = GetHeader(response, "Content-Security-Policy");
            Assert.Contains("default-src 'none'", csp, StringComparison.Ordinal);
            Assert.Contains("nonce-", csp, StringComparison.Ordinal);
        }

        private static string GetHeader(System.Net.Http.HttpResponseMessage response, string name)
        {
            if (response.Headers.Contains(name))
            {
                return string.Join(";", response.Headers.GetValues(name));
            }

            if (response.Content.Headers.Contains(name))
            {
                return string.Join(";", response.Content.Headers.GetValues(name));
            }

            return string.Empty;
        }

        private static WebApplicationFactory<Program> CreateProbeFactory(string environment)
        {
#pragma warning disable CA2000 // The inner factory is disposed with the wrapper.
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
#pragma warning restore CA2000
                .UseEnvironment(environment)
                .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["UseInMemoryDatabase"] = "true",
                    ["EnableProviderStates"] = "true",
                })));
        }
    }
}
