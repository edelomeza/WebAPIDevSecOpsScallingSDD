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

namespace IntegrationTest.Saga
{
    public class VentasPedidoTests
    {
        [Fact]
        public async Task CreateFlowPublishesAndGetById()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "PedFlow", existencia: 5, precio: 10.5m);

            var created = await PostPedidoAsync(client, fks.ClienteId, fks.ProductoId, 2);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var pedidoId = createdDoc.RootElement.GetProperty("id").GetGuid();
            Assert.Equal("Creado", createdDoc.RootElement.GetProperty("strEstadoSaga").GetString());
            Assert.Equal(21m, createdDoc.RootElement.GetProperty("decTotal").GetDecimal());
            var detalles = createdDoc.RootElement.GetProperty("Detalles");
            Assert.Equal(1, detalles.GetArrayLength());
            Assert.Equal(10.5m, detalles[0].GetProperty("decPrecioUnitario").GetDecimal());

            var fetched = await client.GetAsync(new Uri($"/api/v1/ventas/pedido/{pedidoId}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);

            Assert.Equal(5, await GetExistenciaAsync(client, fks.ProductoId));

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task UnknownFkReturnsUnprocessable()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "PedUnp", existencia: 5, precio: 10m);

            var badCliente = await PostPedidoRawAsync(client, $"{{\"idCliCliente\":999999,\"Detalles\":[{{\"idProProducto\":{fks.ProductoId},\"intCantidad\":1}}]}}");
            Assert.Equal(HttpStatusCode.UnprocessableEntity, badCliente.StatusCode);
            var body = await badCliente.Content.ReadAsStringAsync();
            Assert.Contains("error", body, StringComparison.OrdinalIgnoreCase);

            var badProducto = await PostPedidoRawAsync(client, $"{{\"idCliCliente\":{fks.ClienteId},\"Detalles\":[{{\"idProProducto\":999999,\"intCantidad\":1}}]}}");
            Assert.Equal(HttpStatusCode.UnprocessableEntity, badProducto.StatusCode);

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task InvalidPayloadsReturnBadRequest()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "PedBad", existencia: 5, precio: 10m);

            var zeroId = await PostPedidoRawAsync(client, $"{{\"idCliCliente\":0,\"Detalles\":[{{\"idProProducto\":{fks.ProductoId},\"intCantidad\":1}}]}}");
            Assert.Equal(HttpStatusCode.BadRequest, zeroId.StatusCode);

            var emptyDetalles = await PostPedidoRawAsync(client, $"{{\"idCliCliente\":{fks.ClienteId},\"Detalles\":[]}}");
            Assert.Equal(HttpStatusCode.BadRequest, emptyDetalles.StatusCode);

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task GetMissingReturnsNotFound()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var missing = await client.GetAsync(new Uri($"/api/v1/ventas/pedido/{Guid.NewGuid()}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }

        [Fact]
        public async Task UserRoleIsForbidden()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "User");

            var response = await client.GetAsync(new Uri($"/api/v1/ventas/pedido/{Guid.NewGuid()}", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private sealed record Fks(int ClienteId, string ClienteRow, int ProductoId, string ProductoRow);

        private static async Task<Fks> SeedFksAsync(HttpClient client, string tag, int existencia, decimal precio)
        {
            using var clienteContent = new StringContent(
                $"{{\"strNombreCliente\":\"Pedido{tag}\",\"strCorreoElectronico\":\"pedido{tag}@test.local\",\"strNumeroTelefono\":\"5550000001\"}}",
                Encoding.UTF8,
                "application/json");
            var cliente = await client.PostAsync(new Uri("/api/v1/clientes", UriKind.Relative), clienteContent).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, cliente.StatusCode);
            using var clienteDoc = JsonDocument.Parse(await cliente.Content.ReadAsStringAsync().ConfigureAwait(false));
            var clienteId = clienteDoc.RootElement.GetProperty("id").GetInt32();
            var clienteRow = clienteDoc.RootElement.GetProperty("RowVersion").GetString()!;

            using var productoContent = new StringContent(
                $"{{\"strNombreProducto\":\"Pedido{tag}\",\"intNumeroExistencia\":{existencia},\"decPrecio\":{precio.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}",
                Encoding.UTF8,
                "application/json");
            var producto = await client.PostAsync(new Uri("/api/v1/productos", UriKind.Relative), productoContent).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, producto.StatusCode);
            using var productoDoc = JsonDocument.Parse(await producto.Content.ReadAsStringAsync().ConfigureAwait(false));
            var productoId = productoDoc.RootElement.GetProperty("id").GetInt32();
            var productoRow = productoDoc.RootElement.GetProperty("RowVersion").GetString()!;

            return new Fks(clienteId, clienteRow, productoId, productoRow);
        }

        private static async Task CleanupFksAsync(HttpClient client, Fks fks)
        {
            await SendDeleteAsync(client, $"/api/v1/productos/{fks.ProductoId}", $"{{\"id\":{fks.ProductoId},\"RowVersion\":\"{fks.ProductoRow}\"}}").ConfigureAwait(false);
            await SendDeleteAsync(client, $"/api/v1/clientes/{fks.ClienteId}", $"{{\"id\":{fks.ClienteId},\"RowVersion\":\"{fks.ClienteRow}\"}}").ConfigureAwait(false);
        }

        private static async Task SendDeleteAsync(HttpClient client, string url, string json)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(url, UriKind.Relative))
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            var deleted = await client.SendAsync(request).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        }

        private static async Task<int> GetExistenciaAsync(HttpClient client, int productoId)
        {
            var fetched = await client.GetAsync(new Uri($"/api/v1/productos/{productoId}", UriKind.Relative)).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
            using var doc = JsonDocument.Parse(await fetched.Content.ReadAsStringAsync().ConfigureAwait(false));
            return doc.RootElement.GetProperty("intNumeroExistencia").GetInt32();
        }

        private static async Task<HttpResponseMessage> PostPedidoAsync(HttpClient client, int clienteId, int productoId, int cantidad)
        {
            return await PostPedidoRawAsync(client, $"{{\"idCliCliente\":{clienteId},\"Detalles\":[{{\"idProProducto\":{productoId},\"intCantidad\":{cantidad}}}]}}").ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> PostPedidoRawAsync(HttpClient client, string json)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await client.PostAsync(new Uri("/api/v1/ventas/pedido", UriKind.Relative), content).ConfigureAwait(false);
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
