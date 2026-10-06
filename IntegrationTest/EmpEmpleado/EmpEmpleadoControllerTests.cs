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

namespace IntegrationTest.EmpEmpleado
{
    public class EmpEmpleadoControllerTests
    {
        [Fact]
        public async Task CrudFlowReturnsExpectedStatusCodes()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            using var createContent = new StringContent(
                "{\"strNombre\":\"Ana\",\"strCURP\":\"SAAA260101HDFXXX01\"}",
                Encoding.UTF8,
                "application/json");
            var created = await client.PostAsync(new Uri("/api/v1/empleados", UriKind.Relative), createContent);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var id = createdDoc.RootElement.GetProperty("id").GetInt32();
            var rowVersion = createdDoc.RootElement.GetProperty("RowVersion").GetString();
            Assert.False(string.IsNullOrEmpty(rowVersion));

            var fetched = await client.GetAsync(new Uri($"/api/v1/empleados/{id}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);

            var paged = await client.GetAsync(new Uri("/api/v1/empleados?page=1&pageSize=20", UriKind.Relative));
            var pagedBody = await paged.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, paged.StatusCode);
            Assert.Contains("\"TotalCount\":1", pagedBody, StringComparison.Ordinal);

            using var updateContent = new StringContent(
                $"{{\"id\":{id},\"strNombre\":\"Ana X\",\"strCURP\":\"SAAA260101HDFXXX01\",\"RowVersion\":\"{rowVersion}\"}}",
                Encoding.UTF8,
                "application/json");
            var updated = await client.PutAsync(new Uri($"/api/v1/empleados/{id}", UriKind.Relative), updateContent);
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

            using var staleRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri($"/api/v1/empleados/{id}", UriKind.Relative))
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
            using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri($"/api/v1/empleados/{id}", UriKind.Relative))
            {
                Content = new StringContent(
                    $"{{\"id\":{id},\"RowVersion\":\"{freshVersion}\"}}",
                    Encoding.UTF8,
                    "application/json"),
            };
            var deleted = await client.SendAsync(deleteRequest);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

            var missing = await client.GetAsync(new Uri($"/api/v1/empleados/{id}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }

        [Fact]
        public async Task InvalidPayloadsReturnBadRequest()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            using var badCreate = new StringContent(
                "{\"strNombre\":\"\",\"strCURP\":\"NO-VALIDA\"}",
                Encoding.UTF8,
                "application/json");
            var create = await client.PostAsync(new Uri("/api/v1/empleados", UriKind.Relative), badCreate);
            Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);

            var badPage = await client.GetAsync(new Uri("/api/v1/empleados?page=0&pageSize=500", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, badPage.StatusCode);

            using var mismatch = new StringContent(
                "{\"id\":999,\"strNombre\":\"X\",\"RowVersion\":\"AQ==\"}",
                Encoding.UTF8,
                "application/json");
            var put = await client.PutAsync(new Uri("/api/v1/empleados/1", UriKind.Relative), mismatch);
            Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        }

        [Fact]
        public async Task UnknownTipoEmpleadoReturnsBadRequest()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            using var content = new StringContent(
                "{\"strNombre\":\"Ana\",\"idEmpCatTipoEmpleado\":999}",
                Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync(new Uri("/api/v1/empleados", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SearchFlow()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            using var createContent = new StringContent(
                "{\"strNombre\":\"Mariana\",\"strAPaterno\":\"Garcia\"}",
                Encoding.UTF8,
                "application/json");
            var created = await client.PostAsync(new Uri("/api/v1/empleados", UriKind.Relative), createContent);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var createdId = createdDoc.RootElement.GetProperty("id").GetInt32();
            var createdRowVersion = createdDoc.RootElement.GetProperty("RowVersion").GetString();

            var search = await client.GetAsync(new Uri("/api/v1/empleados/search?texto=Mariana&page=1&pageSize=20", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, search.StatusCode);
            var searchBody = await search.Content.ReadAsStringAsync();
            Assert.Contains("\"TotalCount\":1", searchBody, StringComparison.Ordinal);

            var bySurname = await client.GetAsync(new Uri("/api/v1/empleados/search?texto=Garcia&page=1&pageSize=20", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, bySurname.StatusCode);
            Assert.Contains("\"TotalCount\":1", await bySurname.Content.ReadAsStringAsync(), StringComparison.Ordinal);

            var noFilters = await client.GetAsync(new Uri("/api/v1/empleados/search?page=1&pageSize=20", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, noFilters.StatusCode);

            var unknownTipo = await client.GetAsync(new Uri("/api/v1/empleados/search?idTipoEmpleado=999999&page=1&pageSize=20", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, unknownTipo.StatusCode);
            Assert.Contains("\"TotalCount\":0", await unknownTipo.Content.ReadAsStringAsync(), StringComparison.Ordinal);

            var zeroTipo = await client.GetAsync(new Uri("/api/v1/empleados/search?idTipoEmpleado=0", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, zeroTipo.StatusCode);
            Assert.Contains("error", await zeroTipo.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

            var negativeTipo = await client.GetAsync(new Uri("/api/v1/empleados/search?idTipoEmpleado=-1", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, negativeTipo.StatusCode);

            var badPage = await client.GetAsync(new Uri("/api/v1/empleados/search?texto=Mariana&page=0&pageSize=20", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, badPage.StatusCode);

            using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri($"/api/v1/empleados/{createdId}", UriKind.Relative))
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

            var response = await client.GetAsync(new Uri("/api/v1/empleados", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SearchWithoutAdminRoleIsForbidden()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "User");

            var search = await client.GetAsync(new Uri("/api/v1/empleados/search?texto=Ana", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Forbidden, search.StatusCode);
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
