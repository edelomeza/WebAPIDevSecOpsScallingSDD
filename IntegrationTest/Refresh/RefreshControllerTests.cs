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
using WebAPIDevSecOpsScallingSDD.Services;

namespace IntegrationTest.Refresh
{
    public class RefreshControllerTests
    {
        [Fact]
        public async Task RefreshRotatesAndReuseFails()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var created = await CreateUserAsync(client, "RefreshAna");
            var pair = await CreatePairAsync(factory, created.Id);

            var first = await RefreshAsync(client, pair.RefreshToken);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            using var firstDoc = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
            var newAccess = firstDoc.RootElement.GetProperty("Token").GetString();
            var newRefresh = firstDoc.RootElement.GetProperty("RefreshToken").GetString();
            Assert.False(string.IsNullOrEmpty(newAccess));
            Assert.False(string.IsNullOrEmpty(newRefresh));
            Assert.NotEqual(pair.RefreshToken, newRefresh);

            var reuse = await RefreshAsync(client, pair.RefreshToken);
            Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
            var reuseBody = await reuse.Content.ReadAsStringAsync();
            Assert.Contains("error", reuseBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(pair.RefreshToken, reuseBody, StringComparison.Ordinal);

            var second = await RefreshAsync(client, newRefresh!);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);

            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        [Fact]
        public async Task UnknownRefreshReturnsUnauthorized()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var created = await CreateUserAsync(client, "RefreshBeto");
            var unknown = new string('B', 64);

            var response = await RefreshAsync(client, unknown);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("error", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(unknown, body, StringComparison.Ordinal);

            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        [Fact]
        public async Task InvalidPayloadsReturnBadRequest()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var empty = await RefreshAsync(client, string.Empty);
            Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        }

        [Fact]
        public async Task LogoutRevokesAndRefreshThenFails()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var created = await CreateUserAsync(client, "RefreshCeci");
            var pair = await CreatePairAsync(factory, created.Id);

            var logout = await LogoutAsync(client, pair.RefreshToken);
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

            var refresh = await RefreshAsync(client, pair.RefreshToken);
            Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

            await DeleteUserAsync(client, created.Id, created.RowVersion);
        }

        [Fact]
        public async Task LogoutWithoutAuthReturnsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var logout = await LogoutAsync(client, new string('C', 64));
            Assert.Equal(HttpStatusCode.Unauthorized, logout.StatusCode);
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

        private static async Task<RefreshPair> CreatePairAsync(WebApplicationFactory<Program> factory, int userId)
        {
            using var scope = factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
            return await service.CreateAsync(userId).ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> RefreshAsync(HttpClient client, string refreshToken)
        {
            using var content = new StringContent(
                $"{{\"RefreshToken\":\"{refreshToken}\"}}",
                Encoding.UTF8,
                "application/json");
            return await client.PostAsync(new Uri("/api/v1/auth/refresh", UriKind.Relative), content).ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> LogoutAsync(HttpClient client, string refreshToken)
        {
            using var content = new StringContent(
                $"{{\"RefreshToken\":\"{refreshToken}\"}}",
                Encoding.UTF8,
                "application/json");
            return await client.PostAsync(new Uri("/api/v1/auth/logout", UriKind.Relative), content).ConfigureAwait(false);
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
