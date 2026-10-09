using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IntegrationTest.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTest.SegUsuario
{
    public class SegUsuarioControllerTests
    {
        [Fact]
        public async Task CrudFlowReturnsExpectedStatusCodes()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            using var createContent = new StringContent(
                "{\"strNombre\":\"Ana\",\"strCorreoElectronico\":\"ana@example.com\",\"strPasswordPlano\":\"Secreto123\"}",
                Encoding.UTF8,
                "application/json");
            var created = await client.PostAsync(new Uri("/api/v1/usuarios", UriKind.Relative), createContent);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var createdBody = await created.Content.ReadAsStringAsync();
            Assert.DoesNotContain("strPWD", createdBody, StringComparison.Ordinal);
            Assert.DoesNotContain("str2FASecreto", createdBody, StringComparison.Ordinal);
            Assert.DoesNotContain("Secreto123", createdBody, StringComparison.Ordinal);
            using var createdDoc = JsonDocument.Parse(createdBody);
            var id = createdDoc.RootElement.GetProperty("id").GetInt32();
            var rowVersion = createdDoc.RootElement.GetProperty("RowVersion").GetString();
            Assert.False(string.IsNullOrEmpty(rowVersion));

            var fetched = await client.GetAsync(new Uri($"/api/v1/usuarios/{id}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
            var fetchedBody = await fetched.Content.ReadAsStringAsync();
            Assert.DoesNotContain("strPWD", fetchedBody, StringComparison.Ordinal);
            Assert.DoesNotContain("str2FASecreto", fetchedBody, StringComparison.Ordinal);

            var paged = await client.GetAsync(new Uri("/api/v1/usuarios?page=1&pageSize=20", UriKind.Relative));
            var pagedBody = await paged.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, paged.StatusCode);
            Assert.Contains("\"TotalCount\":1", pagedBody, StringComparison.Ordinal);

            using var updateContent = new StringContent(
                $"{{\"id\":{id},\"strNombre\":\"Ana X\",\"strCorreoElectronico\":\"anax@example.com\",\"RowVersion\":\"{rowVersion}\"}}",
                Encoding.UTF8,
                "application/json");
            var updated = await client.PutAsync(new Uri($"/api/v1/usuarios/{id}", UriKind.Relative), updateContent);
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

            using var staleRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri($"/api/v1/usuarios/{id}", UriKind.Relative))
            {
                Content = new StringContent(
                    $"{{\"id\":{id},\"RowVersion\":\"CQ==\"}}",
                    Encoding.UTF8,
                    "application/json"),
            };
            var stale = await client.SendAsync(staleRequest);
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

            using var freshDoc = JsonDocument.Parse(await updated.Content.ReadAsStringAsync());
            var freshVersion = freshDoc.RootElement.GetProperty("RowVersion").GetString();
            using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri($"/api/v1/usuarios/{id}", UriKind.Relative))
            {
                Content = new StringContent(
                    $"{{\"id\":{id},\"RowVersion\":\"{freshVersion}\"}}",
                    Encoding.UTF8,
                    "application/json"),
            };
            var deleted = await client.SendAsync(deleteRequest);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

            var missing = await client.GetAsync(new Uri($"/api/v1/usuarios/{id}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }

        [Fact]
        public async Task SearchAndAutocompleteFlow()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            using var createContent = new StringContent(
                "{\"strNombre\":\"Mariana\",\"strCorreoElectronico\":\"mariana@example.com\",\"strPasswordPlano\":\"Secreto123\"}",
                Encoding.UTF8,
                "application/json");
            var created = await client.PostAsync(new Uri("/api/v1/usuarios", UriKind.Relative), createContent);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);

            var search = await client.GetAsync(new Uri("/api/v1/usuarios/search?texto=Mariana&page=1&pageSize=20", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, search.StatusCode);
            var searchBody = await search.Content.ReadAsStringAsync();
            Assert.Contains("\"TotalCount\":1", searchBody, StringComparison.Ordinal);
            Assert.DoesNotContain("strPWD", searchBody, StringComparison.Ordinal);
            Assert.DoesNotContain("str2FASecreto", searchBody, StringComparison.Ordinal);
            Assert.DoesNotContain("Secreto123", searchBody, StringComparison.Ordinal);

            var noTexto = await client.GetAsync(new Uri("/api/v1/usuarios/search?page=1&pageSize=20", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, noTexto.StatusCode);
            var noTextoBody = await noTexto.Content.ReadAsStringAsync();
            Assert.Contains("error", noTextoBody, StringComparison.OrdinalIgnoreCase);

            var autocomplete = await client.GetAsync(new Uri("/api/v1/usuarios/autocomplete?texto=Mar&maxResultados=5", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, autocomplete.StatusCode);
            var autoBody = await autocomplete.Content.ReadAsStringAsync();
            using var autoDoc = JsonDocument.Parse(autoBody);
            Assert.True(autoDoc.RootElement.GetArrayLength() <= 5);
            Assert.DoesNotContain("strPWD", autoBody, StringComparison.Ordinal);
            Assert.DoesNotContain("strCorreo", autoBody, StringComparison.Ordinal);

            var autoNoTexto = await client.GetAsync(new Uri("/api/v1/usuarios/autocomplete?maxResultados=5", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, autoNoTexto.StatusCode);

            var autoNormalized = await client.GetAsync(new Uri("/api/v1/usuarios/autocomplete?texto=Mar&maxResultados=0", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, autoNormalized.StatusCode);
            var autoNormalizedOver = await client.GetAsync(new Uri("/api/v1/usuarios/autocomplete?texto=Mar&maxResultados=51", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, autoNormalizedOver.StatusCode);
        }

        [Fact]
        public async Task InvalidPayloadsReturnBadRequest()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            using var badCreate = new StringContent(
                "{\"strNombre\":\"\",\"strCorreoElectronico\":\"no-es-correo\",\"strPasswordPlano\":\"corto\"}",
                Encoding.UTF8,
                "application/json");
            var create = await client.PostAsync(new Uri("/api/v1/usuarios", UriKind.Relative), badCreate);
            Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);

            var badPage = await client.GetAsync(new Uri("/api/v1/usuarios?page=0&pageSize=500", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, badPage.StatusCode);

            using var mismatch = new StringContent(
                "{\"id\":999,\"strNombre\":\"X\",\"strCorreoElectronico\":\"x@example.com\",\"RowVersion\":\"AQ==\"}",
                Encoding.UTF8,
                "application/json");
            var put = await client.PutAsync(new Uri("/api/v1/usuarios/1", UriKind.Relative), mismatch);
            Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        }

        [Fact]
        public async Task AuthenticatedWithoutAdminRoleIsForbidden()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "User");

            var response = await client.GetAsync(new Uri("/api/v1/usuarios", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SearchWithoutAdminRoleIsForbidden()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "User");

            var search = await client.GetAsync(new Uri("/api/v1/usuarios/search?texto=Ana", UriKind.Relative));
            var autocomplete = await client.GetAsync(new Uri("/api/v1/usuarios/autocomplete?texto=Ana", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Forbidden, search.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, autocomplete.StatusCode);
        }

        private static void AddAdminRole(HttpClient client) => client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        private static WebApplicationFactory<Program> CreateAdminFactory()
        {
#pragma warning disable CA2000 // The inner factory is disposed with the wrapper.
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Staging").ConfigureTestServices(services =>
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
