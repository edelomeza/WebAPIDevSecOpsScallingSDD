using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAPIDevSecOpsScallingSDD.Services;

namespace SecurityTest.RateLimit
{
    public class RateLimitTests
    {
        [Fact]
        public async Task LoginPolicyRejectsExcessWithUniform429()
        {
            using var factory = CreateFactory(new Dictionary<string, string?>
            {
                ["RateLimiting:LoginPermitLimit"] = "2",
                ["RateLimiting:LoginWindowSeconds"] = "300",
            });
            using var client = factory.CreateClient();

            var first = await PostLoginAsync(client, "Nadie", "Mala1234");
            Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
            var second = await PostLoginAsync(client, "Nadie", "Mala1234");
            Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);

            var rejected = await PostLoginAsync(client, "Nadie", "Mala1234");
            Assert.Equal((HttpStatusCode)429, rejected.StatusCode);
            await AssertUniform429Async(rejected, "Mala1234");
        }

        [Fact]
        public async Task GlobalPolicyRejectsExcessRefreshWithUniform429()
        {
            using var factory = CreateFactory(new Dictionary<string, string?>
            {
                ["RateLimiting:GlobalPermitLimit"] = "2",
                ["RateLimiting:GlobalWindowSeconds"] = "60",
            });
            using var client = factory.CreateClient();
            var unknown = new string('D', 64);

            var first = await PostRefreshAsync(client, unknown);
            Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
            var second = await PostRefreshAsync(client, unknown);
            Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);

            var rejected = await PostRefreshAsync(client, unknown);
            Assert.Equal((HttpStatusCode)429, rejected.StatusCode);
            var body = await rejected.Content.ReadAsStringAsync();
            Assert.DoesNotContain(unknown, body, StringComparison.Ordinal);
            await AssertUniform429Async(rejected, unknown);
        }

        [Fact]
        public async Task AdminPolicyRejectsExcessWithUniform429()
        {
            using var factory = CreateFactory(new Dictionary<string, string?>
            {
                ["RateLimiting:AdminPermitLimit"] = "2",
                ["RateLimiting:AdminWindowSeconds"] = "60",
            });
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateAdminToken(factory));

            var first = await client.GetAsync(new Uri("/api/v1/clientes?page=1&pageSize=20", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var second = await client.GetAsync(new Uri("/api/v1/clientes?page=1&pageSize=20", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);

            var rejected = await client.GetAsync(new Uri("/api/v1/clientes?page=1&pageSize=20", UriKind.Relative));
            Assert.Equal((HttpStatusCode)429, rejected.StatusCode);
            await AssertUniform429Async(rejected, null);
        }

        [Fact]
        public void EveryControllerActionHasExplicitRateLimitPolicy()
        {
            var allowed = new[]
            {
                RateLimitOptions.LoginPolicyName,
                RateLimitOptions.Login2faPolicyName,
                RateLimitOptions.GlobalPolicyName,
                RateLimitOptions.AdminPolicyName,
                RateLimitOptions.ConcurrentWritesPolicyName,
            };
            var missing = new List<string>();
            var controllers = typeof(Program).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && !string.Equals(t.Name, "PingController", StringComparison.Ordinal));
            foreach (var controller in controllers)
            {
                var controllerPolicy = controller.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName;
                var actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());
                foreach (var action in actions)
                {
                    var policy = action.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName ?? controllerPolicy;
                    if (string.IsNullOrEmpty(policy) || !allowed.Contains(policy, StringComparer.Ordinal))
                    {
                        missing.Add(controller.Name + "." + action.Name + " -> '" + policy + "'");
                    }
                }
            }

            Assert.True(missing.Count == 0, "Sin policy explícita: " + string.Join("; ", missing));
        }

        private static async Task AssertUniform429Async(HttpResponseMessage response, string? secret)
        {
            // Retry-After es best-effort (solo si el limitador provee metadata); no se aserta.
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            Assert.Equal(429, root.GetProperty("Status").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("Error").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("TraceId").GetString()));
            if (!string.IsNullOrEmpty(secret))
            {
                Assert.DoesNotContain(secret, body, StringComparison.Ordinal);
            }
        }

        private static async Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string nombre, string password)
        {
            using var content = new StringContent(
                "{\"strNombre\":\"" + nombre + "\",\"strPasswordPlano\":\"" + password + "\"}",
                Encoding.UTF8,
                "application/json");
            return await client.PostAsync(new Uri("/api/v1/auth/login", UriKind.Relative), content).ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> PostRefreshAsync(HttpClient client, string refreshToken)
        {
            using var content = new StringContent(
                "{\"RefreshToken\":\"" + refreshToken + "\"}",
                Encoding.UTF8,
                "application/json");
            return await client.PostAsync(new Uri("/api/v1/auth/refresh", UriKind.Relative), content).ConfigureAwait(false);
        }

        private static string CreateAdminToken(WebApplicationFactory<Program> factory)
        {
            using var scope = factory.Services.CreateScope();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            return jwt.CreateAccessToken(7, "Admin");
        }

        private static WebApplicationFactory<Program> CreateFactory(Dictionary<string, string?> overrides)
        {
#pragma warning disable CA2000 // La factory se dispone con el using del llamador.
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(
                (_, config) => config.AddInMemoryCollection(overrides)));
#pragma warning restore CA2000
        }
    }
}
