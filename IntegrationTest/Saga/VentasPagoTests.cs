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

namespace IntegrationTest.Saga
{
    public class VentasPagoTests
    {
        [Fact]
        public async Task CreateFlowAndGetByIdAndByPedido()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "PagFlow", existencia: 5, precio: 10.5m);
            var pedidoId = await PostPedidoAsync(client, fks.ClienteId, fks.ProductoId, 1);
            var transaccion = $"TX-{Guid.NewGuid():N}";

            var created = await PostPagoRawAsync(client, $"{{\"idVenPedido\":\"{pedidoId}\",\"decMonto\":10.5,\"strMetodoPago\":\"Efectivo\",\"strIdTransaccion\":\"{transaccion}\"}}");
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var pagoId = createdDoc.RootElement.GetProperty("id").GetInt32();
            Assert.Equal("Procesado", createdDoc.RootElement.GetProperty("strEstado").GetString());
            Assert.Equal(10.5m, createdDoc.RootElement.GetProperty("decMonto").GetDecimal());
            Assert.Equal(transaccion, createdDoc.RootElement.GetProperty("strIdTransaccion").GetString());

            var fetched = await client.GetAsync(new Uri($"/api/v1/ventas/pago/{pagoId}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);

            var byPedido = await client.GetAsync(new Uri($"/api/v1/ventas/pago/pedido/{pedidoId}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, byPedido.StatusCode);
            using var listDoc = JsonDocument.Parse(await byPedido.Content.ReadAsStringAsync());
            Assert.Equal(1, listDoc.RootElement.GetArrayLength());
            Assert.Equal(pagoId, listDoc.RootElement[0].GetProperty("id").GetInt32());

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task DuplicateTransaccionReturnsConflict()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "PagDup", existencia: 5, precio: 10m);
            var pedidoId = await PostPedidoAsync(client, fks.ClienteId, fks.ProductoId, 1);
            var transaccion = $"TX-{Guid.NewGuid():N}";

            var first = await PostPagoRawAsync(client, $"{{\"idVenPedido\":\"{pedidoId}\",\"decMonto\":10,\"strIdTransaccion\":\"{transaccion}\"}}");
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await PostPagoRawAsync(client, $"{{\"idVenPedido\":\"{pedidoId}\",\"decMonto\":10,\"strIdTransaccion\":\"{transaccion}\"}}");
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
            var body = await second.Content.ReadAsStringAsync();
            Assert.Contains("error", body, StringComparison.OrdinalIgnoreCase);

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task UnknownPedidoReturnsUnprocessable()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var missing = Guid.NewGuid();
            var response = await PostPagoRawAsync(client, $"{{\"idVenPedido\":\"{missing}\",\"decMonto\":10}}");
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("error", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task InvalidPayloadsReturnBadRequest()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "PagBad", existencia: 5, precio: 10m);
            var pedidoId = await PostPedidoAsync(client, fks.ClienteId, fks.ProductoId, 1);

            var zeroMonto = await PostPagoRawAsync(client, $"{{\"idVenPedido\":\"{pedidoId}\",\"decMonto\":0}}");
            Assert.Equal(HttpStatusCode.BadRequest, zeroMonto.StatusCode);

            var emptyPedido = await PostPagoRawAsync(client, "{\"idVenPedido\":\"00000000-0000-0000-0000-000000000000\",\"decMonto\":10}");
            Assert.Equal(HttpStatusCode.BadRequest, emptyPedido.StatusCode);

            var longMetodo = await PostPagoRawAsync(client, $"{{\"idVenPedido\":\"{pedidoId}\",\"decMonto\":10,\"strMetodoPago\":\"{new string('M', 51)}\"}}");
            Assert.Equal(HttpStatusCode.BadRequest, longMetodo.StatusCode);

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task GetMissingReturnsNotFound()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var missing = await client.GetAsync(new Uri("/api/v1/ventas/pago/999999", UriKind.Relative));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }

        [Fact]
        public async Task GetByPedidoEmptyReturnsOkEmpty()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var response = await client.GetAsync(new Uri($"/api/v1/ventas/pago/pedido/{Guid.NewGuid()}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(0, doc.RootElement.GetArrayLength());
        }

        [Fact]
        public async Task UserRoleIsForbidden()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "User");

            var get = await client.GetAsync(new Uri("/api/v1/ventas/pago/1", UriKind.Relative));
            Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);

            using var content = new StringContent("{\"idVenPedido\":\"11111111-1111-1111-1111-111111111111\",\"decMonto\":10}", Encoding.UTF8, "application/json");
            var post = await client.PostAsync(new Uri("/api/v1/ventas/pago", UriKind.Relative), content);
            Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
        }

        private sealed record Fks(int ClienteId, string ClienteRow, int ProductoId, string ProductoRow);

        private static async Task<Fks> SeedFksAsync(HttpClient client, string tag, int existencia, decimal precio)
        {
            using var clienteContent = new StringContent(
                $"{{\"strNombreCliente\":\"Pago{tag}\",\"strCorreoElectronico\":\"pago{tag}@test.local\",\"strNumeroTelefono\":\"5550000001\"}}",
                Encoding.UTF8,
                "application/json");
            var cliente = await client.PostAsync(new Uri("/api/v1/clientes", UriKind.Relative), clienteContent).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, cliente.StatusCode);
            using var clienteDoc = JsonDocument.Parse(await cliente.Content.ReadAsStringAsync().ConfigureAwait(false));
            var clienteId = clienteDoc.RootElement.GetProperty("id").GetInt32();
            var clienteRow = clienteDoc.RootElement.GetProperty("RowVersion").GetString()!;

            using var productoContent = new StringContent(
                $"{{\"strNombreProducto\":\"Pago{tag}\",\"intNumeroExistencia\":{existencia},\"decPrecio\":{precio.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}",
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

        private static async Task<Guid> PostPedidoAsync(HttpClient client, int clienteId, int productoId, int cantidad)
        {
            var response = await PostPedidoRawAsync(client, $"{{\"idCliCliente\":{clienteId},\"Detalles\":[{{\"idProProducto\":{productoId},\"intCantidad\":{cantidad}}}]}}").ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            return doc.RootElement.GetProperty("id").GetGuid();
        }

        private static async Task<HttpResponseMessage> PostPedidoRawAsync(HttpClient client, string json)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await client.PostAsync(new Uri("/api/v1/ventas/pedido", UriKind.Relative), content).ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> PostPagoRawAsync(HttpClient client, string json)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await client.PostAsync(new Uri("/api/v1/ventas/pago", UriKind.Relative), content).ConfigureAwait(false);
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
