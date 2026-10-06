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
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTest.Login
{
    public class LoginControllerTests
    {
        [Fact]
        public async Task LoginFlowReturnsToken()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var created = await CreateUserAsync(client, "LoginMariana");
            var login = await LoginAsync(client, "LoginMariana", "Secreto123");

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            Assert.False(string.IsNullOrEmpty(loginDoc.RootElement.GetProperty("Token").GetString()));

            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        [Fact]
        public async Task WrongPasswordAndUnknownUserReturnIdenticalUnauthorized()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var created = await CreateUserAsync(client, "LoginAna");

            var wrong = await LoginAsync(client, "LoginAna", "Mala1234");
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            var wrongBody = await wrong.Content.ReadAsStringAsync();

            var unknown = await LoginAsync(client, "Nadie", "Secreto123");
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

            var created = await CreateUserAsync(client, "LoginBeto");

            for (var i = 0; i < 5; i++)
            {
                var failure = await LoginAsync(client, "LoginBeto", "Mala1234");
                Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
            }

            var locked = await LoginAsync(client, "LoginBeto", "Secreto123");
            Assert.Equal((HttpStatusCode)423, locked.StatusCode);

            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        [Fact]
        public async Task InvalidPayloadsReturnBadRequest()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var empty = await LoginAsync(client, string.Empty, "Secreto123");
            Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
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
            }));
        }
    }
}
