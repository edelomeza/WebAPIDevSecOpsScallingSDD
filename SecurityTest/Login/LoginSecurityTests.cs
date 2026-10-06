using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecurityTest.Login
{
    public class LoginSecurityTests
    {
        [Fact]
        public async Task AnonymousBadLoginIsUnauthorizedWithoutLeaks()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var content = new StringContent(
                "{\"strNombre\":\"Nadie\",\"strPasswordPlano\":\"Mala1234\"}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(new Uri("/api/v1/auth/login", UriKind.Relative), content);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("error", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Mala1234", body, StringComparison.Ordinal);
            Assert.DoesNotContain("strPWD", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AnonymousEmptyLoginIsBadRequest()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var content = new StringContent(
                "{\"strNombre\":\"\",\"strPasswordPlano\":\"\"}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(new Uri("/api/v1/auth/login", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
