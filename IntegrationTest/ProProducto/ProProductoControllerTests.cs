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

namespace IntegrationTest.ProProducto
{
    public class ProProductoControllerTests
    {
        [Fact]
        public async Task CrudFlowReturnsExpectedStatusCodes()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            using var createContent = new StringContent(
                "{\"strNombreProducto\":\"Laptop\",\"intNumeroExistencia\":5,\"decPrecio\":99.99}",
                Encoding.UTF8,
                "application/json");
            var created = await client.PostAsync(new Uri("/api/v1/productos", UriKind.Relative), createContent);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var id = createdDoc.RootElement.GetProperty("id").GetInt32();
            var rowVersion = createdDoc.RootElement.GetProperty("RowVersion").GetString();
            Assert.False(string.IsNullOrEmpty(rowVersion));

            var fetched = await client.GetAsync(new Uri($"/api/v1/productos/{id}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);

            var paged = await client.GetAsync(new Uri("/api/v1/productos?page=1&pageSize=20", UriKind.Relative));
            var pagedBody = await paged.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, paged.StatusCode);
            Assert.Contains("\"TotalCount\":1", pagedBody, StringComparison.Ordinal);

            using var updateContent = new StringContent(
                $"{{\"id\":{id},\"strNombreProducto\":\"Laptop X\",\"intNumeroExistencia\":7,\"decPrecio\":70.00,\"RowVersion\":\"{rowVersion}\"}}",
                Encoding.UTF8,
                "application/json");
            var updated = await client.PutAsync(new Uri($"/api/v1/productos/{id}", UriKind.Relative), updateContent);
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

            using var staleRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri($"/api/v1/productos/{id}", UriKind.Relative))
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
            using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri($"/api/v1/productos/{id}", UriKind.Relative))
            {
                Content = new StringContent(
                    $"{{\"id\":{id},\"RowVersion\":\"{freshVersion}\"}}",
                    Encoding.UTF8,
                    "application/json"),
            };
            var deleted = await client.SendAsync(deleteRequest);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

            var missing = await client.GetAsync(new Uri($"/api/v1/productos/{id}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }

        [Fact]
        public async Task InvalidPayloadsReturnBadRequest()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            using var badCreate = new StringContent(
                "{\"strNombreProducto\":\"\",\"intNumeroExistencia\":-1,\"decPrecio\":-5}",
                Encoding.UTF8,
                "application/json");
            var create = await client.PostAsync(new Uri("/api/v1/productos", UriKind.Relative), badCreate);
            Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);

            var badPage = await client.GetAsync(new Uri("/api/v1/productos?page=0&pageSize=500", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, badPage.StatusCode);

            using var mismatch = new StringContent(
                "{\"id\":999,\"strNombreProducto\":\"X\",\"intNumeroExistencia\":1,\"decPrecio\":10.00,\"RowVersion\":\"AQ==\"}",
                Encoding.UTF8,
                "application/json");
            var put = await client.PutAsync(new Uri("/api/v1/productos/1", UriKind.Relative), mismatch);
            Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        }

        [Fact]
        public async Task SearchByNameFlow()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            using var createContent = new StringContent(
                "{\"strNombreProducto\":\"Tornillo\",\"intNumeroExistencia\":5,\"decPrecio\":99.99}",
                Encoding.UTF8,
                "application/json");
            var created = await client.PostAsync(new Uri("/api/v1/productos", UriKind.Relative), createContent);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var createdId = createdDoc.RootElement.GetProperty("id").GetInt32();
            var createdRowVersion = createdDoc.RootElement.GetProperty("RowVersion").GetString();

            var search = await client.GetAsync(new Uri("/api/v1/productos/search?texto=Tornillo&page=1&pageSize=20", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, search.StatusCode);
            Assert.Contains("\"TotalCount\":1", await search.Content.ReadAsStringAsync(), StringComparison.Ordinal);

            var noTexto = await client.GetAsync(new Uri("/api/v1/productos/search?page=1&pageSize=20", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, noTexto.StatusCode);
            Assert.Contains("error", await noTexto.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

            var tooLong = await client.GetAsync(new Uri("/api/v1/productos/search?texto=123456789012345678901234567890123456789012345678901&page=1&pageSize=20", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);

            var badPage = await client.GetAsync(new Uri("/api/v1/productos/search?texto=Tornillo&page=0&pageSize=20", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, badPage.StatusCode);

            using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri($"/api/v1/productos/{createdId}", UriKind.Relative))
            {
                Content = new StringContent(
                    $"{{\"id\":{createdId},\"RowVersion\":\"{createdRowVersion}\"}}",
                    Encoding.UTF8,
                    "application/json"),
            };
            var deleted = await client.SendAsync(deleteRequest);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        }

        [Fact]
        public async Task AuthenticatedWithoutAdminRoleIsForbidden()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "User");

            var response = await client.GetAsync(new Uri("/api/v1/productos", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SearchWithoutAdminRoleIsForbidden()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "User");

            var search = await client.GetAsync(new Uri("/api/v1/productos/search?texto=Tornillo", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Forbidden, search.StatusCode);
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
