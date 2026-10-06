using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecurityTest.Login2Fa
{
    public class Login2FaSecurityTests
    {
        [Fact]
        public async Task AnonymousBadVerifyIsUnauthorizedWithoutLeaks()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var content = new StringContent(
                $"{{\"TempToken\":\"{new string('A', 64)}\",\"TotpCode\":\"000000\"}}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(new Uri("/api/v1/auth/login2fa/verify", UriKind.Relative), content);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("error", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("000000", body, StringComparison.Ordinal);
            Assert.DoesNotContain("strPWD", body, StringComparison.Ordinal);
            Assert.DoesNotContain("str2FASecreto", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AnonymousEmptyVerifyIsBadRequest()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var content = new StringContent(
                "{\"TempToken\":\"\",\"TotpCode\":\"\"}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(new Uri("/api/v1/auth/login2fa/verify", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
