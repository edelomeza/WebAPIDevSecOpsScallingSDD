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

namespace IntegrationTest.VenCatEstado
{
    public class VenCatEstadoControllerTests
    {
        [Fact]
        public async Task CrudFlowReturnsExpectedStatusCodes()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            using var createContent = new StringContent(
                "{\"strValor\":\"Vigente\",\"strDescripcion\":\"Venta vigente\"}",
                Encoding.UTF8,
                "application/json");
            var created = await client.PostAsync(new Uri("/api/v1/estados-venta", UriKind.Relative), createContent);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var id = createdDoc.RootElement.GetProperty("id").GetInt32();

            var fetched = await client.GetAsync(new Uri($"/api/v1/estados-venta/{id}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);

            var paged = await client.GetAsync(new Uri("/api/v1/estados-venta?page=1&pageSize=20", UriKind.Relative));
            var pagedBody = await paged.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, paged.StatusCode);
            Assert.Contains("\"TotalCount\":1", pagedBody, StringComparison.Ordinal);

            using var updateContent = new StringContent(
                $"{{\"id\":{id},\"strValor\":\"Vigente X\",\"strDescripcion\":\"Actualizada\"}}",
                Encoding.UTF8,
                "application/json");
            var updated = await client.PutAsync(new Uri($"/api/v1/estados-venta/{id}", UriKind.Relative), updateContent);
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

            using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri($"/api/v1/estados-venta/{id}", UriKind.Relative))
            {
                Content = new StringContent(
                    $"{{\"id\":{id}}}",
                    Encoding.UTF8,
                    "application/json"),
            };
            var deleted = await client.SendAsync(deleteRequest);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

            var missing = await client.GetAsync(new Uri($"/api/v1/estados-venta/{id}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }

        [Fact]
        public async Task InvalidPayloadsReturnBadRequest()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            using var badCreate = new StringContent(
                "{\"strValor\":\"\"}",
                Encoding.UTF8,
                "application/json");
            var create = await client.PostAsync(new Uri("/api/v1/estados-venta", UriKind.Relative), badCreate);
            Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);

            var badPage = await client.GetAsync(new Uri("/api/v1/estados-venta?page=0&pageSize=500", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, badPage.StatusCode);

            using var mismatch = new StringContent(
                "{\"id\":999}",
                Encoding.UTF8,
                "application/json");
            var put = await client.PutAsync(new Uri("/api/v1/estados-venta/1", UriKind.Relative), mismatch);
            Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        }

        [Fact]
        public async Task AuthenticatedWithoutAdminRoleIsForbidden()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "User");

            var response = await client.GetAsync(new Uri("/api/v1/estados-venta", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
