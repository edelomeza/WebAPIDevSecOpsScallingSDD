using System;
using System.Linq;
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
using WebAPIDevSecOpsScallingSDD.Context;
using FacturaModel = WebAPIDevSecOpsScallingSDD.Models.VenPedidoFactura;

namespace IntegrationTest.Saga
{
    public class VentasDashboardTests
    {
        [Fact]
        public async Task DashboardFlow()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "DashFlow", existencia: 5, precio: 10.5m);
            var pedidoId = await PostPedidoAsync(client, fks.ClienteId, fks.ProductoId, 1);
            Assert.Equal("StockValidado", await WaitForEstadoAsync(client, pedidoId, "StockValidado"));
            await PostPagoAsync(client, pedidoId, 10.5m);
            InsertFactura(factory, pedidoId, $"F-DASH-{Guid.NewGuid():N}", 10.5m);

            var response = await client.GetAsync(new Uri("/api/v1/ventas/dashboard", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(doc.RootElement.GetProperty("TotalPedidos").GetInt32() >= 1);
            Assert.True(doc.RootElement.GetProperty("TotalPagos").GetInt32() >= 1);
            Assert.True(doc.RootElement.GetProperty("TotalFacturas").GetInt32() >= 1);
            Assert.Equal(0, doc.RootElement.GetProperty("ProfundidadCola").GetInt32());
            // El bus avanza el pedido en segundo plano (06-01): esperar la cadena completa
            // pedido → pago → factura antes de leer el agregado.
            Assert.Equal("Facturado", await WaitForEstadoAsync(client, pedidoId, "Facturado"));
            var dashboard = await client.GetAsync(new Uri("/api/v1/ventas/dashboard", UriKind.Relative));
            using var dashboardDoc = JsonDocument.Parse(await dashboard.Content.ReadAsStringAsync());
            var estados = dashboardDoc.RootElement.GetProperty("PorEstadoSaga").EnumerateArray().Select(e => e.GetProperty("Estado").GetString()).ToList();
            Assert.Contains("Facturado", estados);
            var body = doc.RootElement.GetRawText();
            Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("folio", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("rfc", body, StringComparison.OrdinalIgnoreCase);

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task DashboardFiltersFlow()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "DashFilter", existencia: 5, precio: 7m);
            var pedidoId = await PostPedidoAsync(client, fks.ClienteId, fks.ProductoId, 1);
            Assert.Equal("StockValidado", await WaitForEstadoAsync(client, pedidoId, "StockValidado"));
            await PostPagoAsync(client, pedidoId, 7m);

            // El bus avanza el pedido en segundo plano (06-01): filtrar por el estado terminal.
            Assert.Equal("Facturado", await WaitForEstadoAsync(client, pedidoId, "Facturado"));
            var filtered = await client.GetAsync(new Uri("/api/v1/ventas/dashboard?desde=2000-01-01&hasta=2100-01-01&estadoSaga=Facturado", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
            using var doc = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
            Assert.True(doc.RootElement.GetProperty("TotalPedidos").GetInt32() >= 1);
            foreach (var entry in doc.RootElement.GetProperty("PorEstadoSaga").EnumerateArray())
            {
                Assert.Equal("Facturado", entry.GetProperty("Estado").GetString());
            }

            var inverted = await client.GetAsync(new Uri("/api/v1/ventas/dashboard?desde=2100-01-01&hasta=2000-01-01", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, inverted.StatusCode);

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task UserRoleIsForbidden()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "User");

            var get = await client.GetAsync(new Uri("/api/v1/ventas/dashboard", UriKind.Relative));
            Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
        }

        private static void InsertFactura(WebApplicationFactory<Program> factory, Guid pedidoId, string folio, decimal total)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = new FacturaModel
            {
                idVenPedido = pedidoId,
                strFolioFactura = folio,
                decTotal = total,
                dteFechaEmision = DateTime.UtcNow,
                strEstado = "Emitida",
            };
            db.VenPedidoFacturas.Add(entity);
            db.SaveChanges();
        }

        private sealed record Fks(int ClienteId, string ClienteRow, int ProductoId, string ProductoRow);

        private static async Task<Fks> SeedFksAsync(HttpClient client, string tag, int existencia, decimal precio)
        {
            using var clienteContent = new StringContent(
                $"{{\"strNombreCliente\":\"Dashboard{tag}\",\"strCorreoElectronico\":\"dashboard{tag}@test.local\",\"strNumeroTelefono\":\"5550000001\"}}",
                Encoding.UTF8,
                "application/json");
            var cliente = await client.PostAsync(new Uri("/api/v1/clientes", UriKind.Relative), clienteContent).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, cliente.StatusCode);
            using var clienteDoc = JsonDocument.Parse(await cliente.Content.ReadAsStringAsync().ConfigureAwait(false));
            var clienteId = clienteDoc.RootElement.GetProperty("id").GetInt32();
            var clienteRow = clienteDoc.RootElement.GetProperty("RowVersion").GetString()!;

            using var productoContent = new StringContent(
                $"{{\"strNombreProducto\":\"Dashboard{tag}\",\"intNumeroExistencia\":{existencia},\"decPrecio\":{precio.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}",
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

        private static async Task<string?> WaitForEstadoAsync(HttpClient client, Guid pedidoId, string esperado)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (DateTime.UtcNow < deadline)
            {
                var fetched = await client.GetAsync(new Uri($"/api/v1/ventas/pedido/{pedidoId}", UriKind.Relative)).ConfigureAwait(false);
                Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
                using var doc = JsonDocument.Parse(await fetched.Content.ReadAsStringAsync().ConfigureAwait(false));
                var estado = doc.RootElement.GetProperty("strEstadoSaga").GetString();
                if (string.Equals(estado, esperado, StringComparison.Ordinal))
                {
                    return estado;
                }

                await Task.Delay(200).ConfigureAwait(false);
            }

            return null;
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
