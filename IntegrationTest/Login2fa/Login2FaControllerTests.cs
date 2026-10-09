using System;
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
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Services;

namespace IntegrationTest.Login2Fa
{
    public class Login2FaControllerTests
    {
        [Fact]
        public async Task FullFlowIssuesTempAndVerifies()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var created = await CreateUserAsync(client, "Login2FaAna");
            await Enable2faAsync(factory, "Login2FaAna");

            var login = await LoginAsync(client, "Login2FaAna", "Secreto123");
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            Assert.True(loginDoc.RootElement.GetProperty("Requires2fa").GetBoolean());
            var temp = loginDoc.RootElement.GetProperty("TempToken").GetString();
            Assert.False(string.IsNullOrEmpty(temp));

            var verify = await VerifyAsync(client, temp!, "123456");
            Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
            using var verifyDoc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
            Assert.False(string.IsNullOrEmpty(verifyDoc.RootElement.GetProperty("Token").GetString()));

            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        [Fact]
        public async Task WrongCodeAndUnknownTempReturnIdenticalUnauthorized()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var created = await CreateUserAsync(client, "Login2FaBeto");
            await Enable2faAsync(factory, "Login2FaBeto");

            var login = await LoginAsync(client, "Login2FaBeto", "Secreto123");
            using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            var temp = loginDoc.RootElement.GetProperty("TempToken").GetString()!;

            var wrong = await VerifyAsync(client, temp, "000000");
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            var wrongBody = await wrong.Content.ReadAsStringAsync();

            var unknown = await VerifyAsync(client, new string('A', 64), "000000");
            Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
            var unknownBody = await unknown.Content.ReadAsStringAsync();

            Assert.Equal(wrongBody, unknownBody);
            Assert.Contains("error", wrongBody, StringComparison.OrdinalIgnoreCase);

            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        [Fact]
        public async Task FiveFailuresThenLockedOut()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var created = await CreateUserAsync(client, "Login2FaCid");
            await Enable2faAsync(factory, "Login2FaCid");

            var login = await LoginAsync(client, "Login2FaCid", "Secreto123");
            using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            var temp = loginDoc.RootElement.GetProperty("TempToken").GetString()!;

            for (var i = 0; i < 5; i++)
            {
                var failure = await VerifyAsync(client, temp, "000000");
                Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
            }

            var locked = await VerifyAsync(client, temp, "123456");
            Assert.Equal((HttpStatusCode)423, locked.StatusCode);

            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        [Fact]
        public async Task InvalidPayloadsReturnBadRequest()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();

            var empty = await VerifyAsync(client, string.Empty, string.Empty);
            Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

            var malformed = await VerifyAsync(client, new string('A', 64), "abc");
            Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        }

        private static async Task Enable2faAsync(WebApplicationFactory<Program> factory, string nombre)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var protector = scope.ServiceProvider.GetRequiredService<ITwoFactorSecretProtector>();
            var user = await db.SegUsuarios.FirstAsync(e => e.strNombre == nombre).ConfigureAwait(false);
            user.bln2FAHabilitado = true;
            user.str2FASecreto = protector.Protect("JBSWY3DPEHPK3PXP");
            await db.SaveChangesAsync().ConfigureAwait(false);
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
                    $"{{\"id\":{id},\"RowVersion\":\"{rowVersion}\"}}",
                    Encoding.UTF8,
                    "application/json"),
            };
            var deleted = await client.SendAsync(deleteRequest).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        }

        private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string nombre, string password)
        {
            using var loginContent = new StringContent(
                $"{{\"strNombre\":\"{nombre}\",\"strPasswordPlano\":\"{password}\"}}",
                Encoding.UTF8,
                "application/json");
            return await client.PostAsync(new Uri("/api/v1/auth/login", UriKind.Relative), loginContent).ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> VerifyAsync(HttpClient client, string temp, string code)
        {
            using var verifyContent = new StringContent(
                $"{{\"TempToken\":\"{temp}\",\"TotpCode\":\"{code}\"}}",
                Encoding.UTF8,
                "application/json");
            return await client.PostAsync(new Uri("/api/v1/auth/login2fa/verify", UriKind.Relative), verifyContent).ConfigureAwait(false);
        }

        private static void AddAdminRole(HttpClient client) => client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

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
                // Pin 03-07 a stub determinista; TOTP real solo en TwoFactor (03-09).
                services.AddScoped<ITotpService, FakeTotpService>();
            }));
        }
    }
}
