using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IntegrationTest.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Services;

namespace IntegrationTest.TwoFactor
{
    public class TwoFactorControllerTests
    {
        [Fact]
        public async Task SetupReturnsOtpAuthUriAndStoresProtected()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            var created = await CreateUserAsync(client, "TwoFactorAna");
            SetUserId(client, created.Id);

            var setup = await SetupAsync(client);
            Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
            using var doc = JsonDocument.Parse(await setup.Content.ReadAsStringAsync());
            var secret = doc.RootElement.GetProperty("Secret").GetString()!;
            var uri = doc.RootElement.GetProperty("OtpAuthUri").GetString()!;
            Assert.False(string.IsNullOrEmpty(secret));
            Assert.StartsWith("otpauth://", uri, StringComparison.Ordinal);
            Assert.Contains(secret, uri, StringComparison.Ordinal);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var protector = scope.ServiceProvider.GetRequiredService<ITwoFactorSecretProtector>();
            var user = await db.SegUsuarios.FirstAsync(e => e.id == created.Id);
            Assert.False(user.bln2FAHabilitado);
            Assert.NotEqual(secret, user.str2FASecreto);
            Assert.Equal(secret, protector.Unprotect(user.str2FASecreto!));

            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        [Fact]
        public async Task VerifyRealTotpEnables2Fa()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            var created = await CreateUserAsync(client, "TwoFactorBeto");
            SetUserId(client, created.Id);

            var secret = await SetupAndGetSecretAsync(client);
            var code = ComputeCode(secret);
            var verify = await VerifyAsync(client, code);

            Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
            using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
            Assert.True(doc.RootElement.GetProperty("Enabled").GetBoolean());

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.True((await db.SegUsuarios.FirstAsync(e => e.id == created.Id)).bln2FAHabilitado);

            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        [Fact]
        public async Task WrongCodeAndUnknownUserReturnIdenticalUnauthorized()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            var created = await CreateUserAsync(client, "TwoFactorCid");
            SetUserId(client, created.Id);
            var secret = await SetupAndGetSecretAsync(client);

            var wrong = await VerifyAsync(client, "000000");
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            var wrongBody = await wrong.Content.ReadAsStringAsync();

            SetUserId(client, 999999);
            var unknown = await VerifyAsync(client, "000000");
            Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
            var unknownBody = await unknown.Content.ReadAsStringAsync();

            Assert.Equal(wrongBody, unknownBody);
            Assert.Contains("error", wrongBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(secret, wrongBody, StringComparison.Ordinal);

            SetUserId(client, created.Id);
            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        [Fact]
        public async Task FiveFailuresThenLockedOut()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            var created = await CreateUserAsync(client, "TwoFactorDan");
            SetUserId(client, created.Id);
            var secret = await SetupAndGetSecretAsync(client);

            for (var i = 0; i < 5; i++)
            {
                var failure = await VerifyAsync(client, "000000");
                Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
            }

            var locked = await VerifyAsync(client, ComputeCode(secret));
            Assert.Equal((HttpStatusCode)423, locked.StatusCode);
            var lockedBody = await locked.Content.ReadAsStringAsync();
            Assert.DoesNotContain(secret, lockedBody, StringComparison.Ordinal);

            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        [Fact]
        public async Task InvalidPayloadsReturnBadRequestAndAnonymousIsUnauthorized()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            var created = await CreateUserAsync(client, "TwoFactorEva");
            SetUserId(client, created.Id);

            var malformed = await VerifyAsync(client, "abc");
            Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);

            using var anonymousFactory = new WebApplicationFactory<Program>();
            using var anonymous = anonymousFactory.CreateClient();
            using var anonContent = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            var anonSetup = await anonymous.PostAsync(
                new Uri("/api/v1/two-factor/setup", UriKind.Relative),
                anonContent).ConfigureAwait(true);
            Assert.Equal(HttpStatusCode.Unauthorized, anonSetup.StatusCode);

            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        [Fact]
        public async Task ResestupInvalidatesPreviousAndDoesNotResetAttempts()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            var created = await CreateUserAsync(client, "TwoFactorGil");
            SetUserId(client, created.Id);

            var first = await SetupAndGetSecretAsync(client);
            var firstCode = ComputeCode(first);
            await VerifyAsync(client, "000000");
            await VerifyAsync(client, "000000");

            var second = await SetupAndGetSecretAsync(client);
            Assert.NotEqual(first, second);

            var oldFails = await VerifyAsync(client, firstCode);
            Assert.Equal(HttpStatusCode.Unauthorized, oldFails.StatusCode);

            var current = await VerifyAsync(client, ComputeCode(second));
            Assert.Equal(HttpStatusCode.OK, current.StatusCode);

            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        private static string ComputeCode(string secret)
        {
            return new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();
        }

        private static async Task<string> SetupAndGetSecretAsync(HttpClient client)
        {
            var setup = await SetupAsync(client).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
            using var doc = JsonDocument.Parse(await setup.Content.ReadAsStringAsync().ConfigureAwait(false));
            return doc.RootElement.GetProperty("Secret").GetString()!;
        }

        private static async Task<HttpResponseMessage> SetupAsync(HttpClient client)
        {
            using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            return await client.PostAsync(
                new Uri("/api/v1/two-factor/setup", UriKind.Relative),
                content).ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> VerifyAsync(HttpClient client, string code)
        {
            using var content = new StringContent(
                $"{{\"TotpCode\":\"{code}\"}}",
                Encoding.UTF8,
                "application/json");
            return await client.PostAsync(new Uri("/api/v1/two-factor/verify", UriKind.Relative), content).ConfigureAwait(false);
        }

        private static void SetUserId(HttpClient client, int userId)
        {
            if (client.DefaultRequestHeaders.Contains("X-Test-UserId"))
            {
                client.DefaultRequestHeaders.Remove("X-Test-UserId");
            }

            client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString(CultureInfo.InvariantCulture));
        }

        private static async Task<(int Id, string RowVersion)> CreateUserAsync(HttpClient client, string nombre)
        {
            using var createContent = new StringContent(
                $"{{\"strNombre\":\"{nombre}\",\"strCorreoElectronico\":\"{nombre}@test.local\",\"strPasswordPlano\":\"Secreto123\"}}",
                Encoding.UTF8,
                "application/json");
            var created = await client.PostAsync(new Uri("/api/v1/usuarios", UriKind.Relative), createContent).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync().ConfigureAwait(false));
            return (createdDoc.RootElement.GetProperty("id").GetInt32(), createdDoc.RootElement.GetProperty("RowVersion").GetString()!);
        }

        private static async Task DeleteUserAsync(HttpClient client, int id, string rowVersion)
        {
            using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri($"/api/v1/usuarios/{id}", UriKind.Relative))
            {
                Content = new StringContent(
                    $"{{\"id\":{id.ToString(CultureInfo.InvariantCulture)},\"RowVersion\":\"{rowVersion}\"}}",
                    Encoding.UTF8,
                    "application/json"),
            };
            var deleted = await client.SendAsync(deleteRequest).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        }

        private static WebApplicationFactory<Program> CreateAdminFactory()
        {
#pragma warning disable CA2000 // The inner factory is disposed with the wrapper.
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
#pragma warning restore CA2000
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            }));
        }
    }
}
