using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IntegrationTest.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTest.Errors
{
    public class ErrorHandlingTests
    {
        [Fact]
        public async Task NotFoundReturnsUniformBody()
        {
            using var factory = CreateProbeFactory("Staging");
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

            var missing = await client.GetAsync(new Uri("/api/v1/clientes/999999", UriKind.Relative));

            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            using var doc = JsonDocument.Parse(await missing.Content.ReadAsStringAsync());
            Assert.Equal(404, doc.RootElement.GetProperty("Status").GetInt32());
            Assert.Contains("no encontrado", doc.RootElement.GetProperty("Error").GetString(), StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrEmpty(doc.RootElement.GetProperty("TraceId").GetString()));
            Assert.False(string.IsNullOrEmpty(doc.RootElement.GetProperty("Detail").GetString()));
        }

        [Fact]
        public async Task ProductionOmitsDetail()
        {
            using var factory = CreateProbeFactory("Production");
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

            var missing = await client.GetAsync(new Uri("/api/v1/clientes/999999", UriKind.Relative));

            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            using var doc = JsonDocument.Parse(await missing.Content.ReadAsStringAsync());
            Assert.Equal(404, doc.RootElement.GetProperty("Status").GetInt32());
            Assert.False(doc.RootElement.TryGetProperty("Detail", out _));
        }

        [Fact]
        public async Task ForbiddenProbeReturnsUniformBody()
        {
            using var factory = CreateProbeFactory("Staging");
            using var client = factory.CreateClient();

            var forbidden = await client.GetAsync(new Uri("/api/v1/probe/forbidden", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            using var doc = JsonDocument.Parse(await forbidden.Content.ReadAsStringAsync());
            Assert.Equal(403, doc.RootElement.GetProperty("Status").GetInt32());
            Assert.Equal("Probe forbidden.", doc.RootElement.GetProperty("Error").GetString());
        }

        [Fact]
        public async Task ConflictRealFlowReturnsUniformBody()
        {
            using var factory = CreateProbeFactory("Staging");
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

            using var createContent = new StringContent(
                "{\"strNombreCliente\":\"ErrCli\",\"strCorreoElectronico\":\"errcli@test.local\",\"strNumeroTelefono\":\"5550000001\"}",
                Encoding.UTF8,
                "application/json");
            var created = await client.PostAsync(new Uri("/api/v1/clientes", UriKind.Relative), createContent);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var id = createdDoc.RootElement.GetProperty("id").GetInt32();
            var rowVersion = createdDoc.RootElement.GetProperty("RowVersion").GetString()!;

            using var updateContent = new StringContent(
                $"{{\"id\":{id},\"strNombreCliente\":\"ErrCli X\",\"strCorreoElectronico\":\"errcli@test.local\",\"strNumeroTelefono\":\"5550000001\",\"RowVersion\":\"{rowVersion}\"}}",
                Encoding.UTF8,
                "application/json");
            var updated = await client.PutAsync(new Uri($"/api/v1/clientes/{id}", UriKind.Relative), updateContent);
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

            using var staleRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri($"/api/v1/clientes/{id}", UriKind.Relative))
            {
                Content = new StringContent(
                    $"{{\"id\":{id},\"RowVersion\":\"CQ==\"}}",
                    Encoding.UTF8,
                    "application/json"),
            };
            var stale = await client.SendAsync(staleRequest);

            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            using var doc = JsonDocument.Parse(await stale.Content.ReadAsStringAsync());
            Assert.Equal(409, doc.RootElement.GetProperty("Status").GetInt32());
            Assert.Equal("El recurso fue modificado por otro proceso.", doc.RootElement.GetProperty("Error").GetString());

            using var freshDoc = JsonDocument.Parse(await updated.Content.ReadAsStringAsync());
            var freshVersion = freshDoc.RootElement.GetProperty("RowVersion").GetString()!;
            using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri($"/api/v1/clientes/{id}", UriKind.Relative))
            {
                Content = new StringContent(
                    $"{{\"id\":{id},\"RowVersion\":\"{freshVersion}\"}}",
                    Encoding.UTF8,
                    "application/json"),
            };
            var deleted = await client.SendAsync(deleteRequest);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        }

        [Fact]
        public async Task ValidationRealFlowReturnsUniformBody()
        {
            using var factory = CreateProbeFactory("Staging");
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

            using var content = new StringContent(
                "{\"strNombre\":\"ErrEmp\",\"idEmpCatTipoEmpleado\":999}",
                Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync(new Uri("/api/v1/empleados", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(422, doc.RootElement.GetProperty("Status").GetInt32());
            Assert.Contains("999", doc.RootElement.GetProperty("Error").GetString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task TimeoutProbeReturnsRequestTimeout()
        {
            using var factory = CreateProbeFactory("Staging");
            using var client = factory.CreateClient();

            var timeout = await client.GetAsync(new Uri("/api/v1/probe/timeout", UriKind.Relative));

            Assert.Equal(HttpStatusCode.RequestTimeout, timeout.StatusCode);
            using var doc = JsonDocument.Parse(await timeout.Content.ReadAsStringAsync());
            Assert.Equal(408, doc.RootElement.GetProperty("Status").GetInt32());
        }

        [Fact]
        public async Task ErrorProbeReturnsGenericWithoutLeak()
        {
            using var factory = CreateProbeFactory("Staging");
            using var client = factory.CreateClient();

            var error = await client.GetAsync(new Uri("/api/v1/probe/error", UriKind.Relative));

            Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);
            using var doc = JsonDocument.Parse(await error.Content.ReadAsStringAsync());
            Assert.Equal(500, doc.RootElement.GetProperty("Status").GetInt32());
            Assert.Equal("Un error interno ha ocurrido en el servidor.", doc.RootElement.GetProperty("Error").GetString());
            Assert.Equal("Probe error.", doc.RootElement.GetProperty("Detail").GetString());
        }

        [Fact]
        public async Task ProbeDisabledInProduction()
        {
            using var factory = CreateProbeFactory("Production");
            using var client = factory.CreateClient();

            var timeout = await client.GetAsync(new Uri("/api/v1/probe/timeout", UriKind.Relative));

            Assert.Equal(HttpStatusCode.NotFound, timeout.StatusCode);
        }

        private static WebApplicationFactory<Program> CreateProbeFactory(string environment)
        {
#pragma warning disable CA2000 // The inner factory is disposed with the wrapper.
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
#pragma warning restore CA2000
                .UseEnvironment(environment)
                .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["UseInMemoryDatabase"] = "true",
                    ["EnableProviderStates"] = "true",
                }))
                .ConfigureTestServices(services =>
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
