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
    // 06-01: el bus InMemory mueve la saga de punta a punta en local.
    public class TransportTests
    {
        [Fact]
        public async Task HappyPathReachesFacturado()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "BusHappy", existencia: 5, precio: 10m);
            var pedidoId = await PostPedidoAsync(client, fks.ClienteId, fks.ProductoId, 2);

            Assert.Equal("StockValidado", await WaitForEstadoAsync(client, pedidoId, "StockValidado"));
            Assert.Equal(3, await GetExistenciaAsync(client, fks.ProductoId));

            // El cobro solo se admite sobre stock validado: sin esta espera el bus rechaza
            // el pago fuera de orden (PagoRechazadoEvent → Cancelado).
            await PostPagoAsync(client, pedidoId, 20m);

            Assert.Equal("Facturado", await WaitForEstadoAsync(client, pedidoId, "Facturado"));
            Assert.Equal(3, await GetExistenciaAsync(client, fks.ProductoId));

            var byPedido = await client.GetAsync(new Uri($"/api/v1/ventas/pago/pedido/{pedidoId}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, byPedido.StatusCode);
            using var listDoc = JsonDocument.Parse(await byPedido.Content.ReadAsStringAsync());
            Assert.Equal("Procesado", listDoc.RootElement[0].GetProperty("strEstado").GetString());

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task WithoutStockCancelsAndKeepsExistencia()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "BusReject", existencia: 5, precio: 10m);
            var pedidoId = await PostPedidoAsync(client, fks.ClienteId, fks.ProductoId, 99);

            Assert.Equal("Cancelado", await WaitForEstadoAsync(client, pedidoId, "Cancelado"));
            Assert.Equal(5, await GetExistenciaAsync(client, fks.ProductoId));

            await CleanupFksAsync(client, fks);
        }

        private sealed record Fks(int ClienteId, string ClienteRow, int ProductoId, string ProductoRow);

        private static async Task<Fks> SeedFksAsync(HttpClient client, string tag, int existencia, decimal precio)
        {
            using var clienteContent = new StringContent(
                $"{{\"strNombreCliente\":\"Bus{tag}\",\"strCorreoElectronico\":\"bus{tag}@test.local\",\"strNumeroTelefono\":\"5550000001\"}}",
                Encoding.UTF8,
                "application/json");
            var cliente = await client.PostAsync(new Uri("/api/v1/clientes", UriKind.Relative), clienteContent).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, cliente.StatusCode);
            using var clienteDoc = JsonDocument.Parse(await cliente.Content.ReadAsStringAsync().ConfigureAwait(false));
            var clienteId = clienteDoc.RootElement.GetProperty("id").GetInt32();
            var clienteRow = clienteDoc.RootElement.GetProperty("RowVersion").GetString()!;

            using var productoContent = new StringContent(
                $"{{\"strNombreProducto\":\"Bus{tag}\",\"intNumeroExistencia\":{existencia},\"decPrecio\":{precio.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}",
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
            using var content = new StringContent(
                $"{{\"idCliCliente\":{clienteId},\"Detalles\":[{{\"idProProducto\":{productoId},\"intCantidad\":{cantidad}}}]}}",
                Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync(new Uri("/api/v1/ventas/pedido", UriKind.Relative), content).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            Assert.Equal("Creado", doc.RootElement.GetProperty("strEstadoSaga").GetString());
            return doc.RootElement.GetProperty("id").GetGuid();
        }

        private static async Task PostPagoAsync(HttpClient client, Guid pedidoId, decimal monto)
        {
            using var content = new StringContent(
                $"{{\"idVenPedido\":\"{pedidoId}\",\"decMonto\":{monto.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}",
                Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync(new Uri("/api/v1/ventas/pago", UriKind.Relative), content).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        private static async Task<int> GetExistenciaAsync(HttpClient client, int productoId)
        {
            var fetched = await client.GetAsync(new Uri($"/api/v1/productos/{productoId}", UriKind.Relative)).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
            using var doc = JsonDocument.Parse(await fetched.Content.ReadAsStringAsync().ConfigureAwait(false));
            return doc.RootElement.GetProperty("intNumeroExistencia").GetInt32();
        }

        private static async Task<string> WaitForEstadoAsync(HttpClient client, Guid pedidoId, string esperado)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            var seen = new System.Collections.Generic.List<string?>();
            while (DateTime.UtcNow < deadline)
            {
                var fetched = await client.GetAsync(new Uri($"/api/v1/ventas/pedido/{pedidoId}", UriKind.Relative)).ConfigureAwait(false);
                Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
                using var doc = JsonDocument.Parse(await fetched.Content.ReadAsStringAsync().ConfigureAwait(false));
                var estado = doc.RootElement.GetProperty("strEstadoSaga").GetString();
                if (!seen.Contains(estado))
                {
                    seen.Add(estado);
                }

                if (string.Equals(estado, esperado, StringComparison.Ordinal))
                {
                    return estado!;
                }

                await Task.Delay(200).ConfigureAwait(false);
            }

            Assert.Fail($"Estado '{esperado}' no alcanzado para {pedidoId}; vistos: {string.Join(",", seen)}.");
            throw new InvalidOperationException("Inalcanzable.");
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
