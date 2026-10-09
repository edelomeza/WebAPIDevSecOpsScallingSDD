using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecurityTest.TwoFactor
{
    public class TwoFactorSecurityTests
    {
        [Fact]
        public async Task AnonymousSetupAndVerifyAreUnauthorizedWithoutLeaks()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var setupContent = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            var setup = await client.PostAsync(new Uri("/api/v1/two-factor/setup", UriKind.Relative), setupContent);
            using var verifyContent = new StringContent("{\"TotpCode\":\"000000\"}", Encoding.UTF8, "application/json");
            var verify = await client.PostAsync(new Uri("/api/v1/two-factor/verify", UriKind.Relative), verifyContent);

            Assert.Equal(HttpStatusCode.Unauthorized, setup.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, verify.StatusCode);
            var setupBody = await setup.Content.ReadAsStringAsync();
            var verifyBody = await verify.Content.ReadAsStringAsync();
            // Challenge [Authorize] sin cuerpo: lo importante es 401 idéntico y sin fugas.
            Assert.Equal(setupBody, verifyBody);
            Assert.DoesNotContain("str2FASecreto", setupBody, StringComparison.Ordinal);
            Assert.DoesNotContain("strPWD", setupBody, StringComparison.Ordinal);
            Assert.DoesNotContain("000000", verifyBody, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AnonymousEmptyVerifyIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var content = new StringContent("{\"TotpCode\":\"\"}", Encoding.UTF8, "application/json");

            var response = await client.PostAsync(new Uri("/api/v1/two-factor/verify", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
