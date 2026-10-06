using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecurityTest.Refresh
{
    public class RefreshSecurityTests
    {
        [Fact]
        public async Task AnonymousBadRefreshIsUnauthorizedWithoutLeaks()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            var sent = new string('D', 64);
            using var content = new StringContent(
                $"{{\"RefreshToken\":\"{sent}\"}}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(new Uri("/api/v1/auth/refresh", UriKind.Relative), content);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("error", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(sent, body, StringComparison.Ordinal);
            Assert.DoesNotContain("strTokenHash", body, StringComparison.Ordinal);
            Assert.DoesNotContain("strPWD", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AnonymousEmptyRefreshIsBadRequest()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var content = new StringContent(
                "{\"RefreshToken\":\"\"}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(new Uri("/api/v1/auth/refresh", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AnonymousLogoutIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var content = new StringContent(
                $"{{\"RefreshToken\":\"{new string('E', 64)}\"}}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(new Uri("/api/v1/auth/logout", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
