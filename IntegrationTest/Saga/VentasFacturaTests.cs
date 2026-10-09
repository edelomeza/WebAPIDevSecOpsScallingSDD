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
using WebAPIDevSecOpsScallingSDD.Context;
using FacturaModel = WebAPIDevSecOpsScallingSDD.Models.VenPedidoFactura;

namespace IntegrationTest.Saga
{
    public class VentasFacturaTests
    {
        [Fact]
        public async Task GetByIdFlow()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "FacFlow", existencia: 5, precio: 10.5m);
            var pedidoId = await PostPedidoAsync(client, fks.ClienteId, fks.ProductoId, 1);
            var folio = $"F-TEST-{Guid.NewGuid():N}";
            var facturaId = InsertFactura(factory, pedidoId, folio, 10.5m);

            var fetched = await client.GetAsync(new Uri($"/api/v1/ventas/factura/{facturaId}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
            using var doc = JsonDocument.Parse(await fetched.Content.ReadAsStringAsync());
            Assert.Equal(facturaId, doc.RootElement.GetProperty("id").GetInt32());
            Assert.Equal(pedidoId, doc.RootElement.GetProperty("idVenPedido").GetGuid());
            Assert.Equal(folio, doc.RootElement.GetProperty("strFolioFactura").GetString());
            Assert.Equal(10.5m, doc.RootElement.GetProperty("decTotal").GetDecimal());
            Assert.Equal("Emitida", doc.RootElement.GetProperty("strEstado").GetString());

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task GetMissingReturnsNotFound()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var missing = await client.GetAsync(new Uri("/api/v1/ventas/factura/999999", UriKind.Relative));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }

        [Fact]
        public async Task UserRoleIsForbidden()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "User");

            var get = await client.GetAsync(new Uri("/api/v1/ventas/factura/1", UriKind.Relative));
            Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
        }

        private static int InsertFactura(WebApplicationFactory<Program> factory, Guid pedidoId, string folio, decimal total)
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
            return entity.id;
        }

        private sealed record Fks(int ClienteId, string ClienteRow, int ProductoId, string ProductoRow);

        private static async Task<Fks> SeedFksAsync(HttpClient client, string tag, int existencia, decimal precio)
        {
            using var clienteContent = new StringContent(
                $"{{\"strNombreCliente\":\"Factura{tag}\",\"strCorreoElectronico\":\"factura{tag}@test.local\",\"strNumeroTelefono\":\"5550000001\"}}",
                Encoding.UTF8,
                "application/json");
            var cliente = await client.PostAsync(new Uri("/api/v1/clientes", UriKind.Relative), clienteContent).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, cliente.StatusCode);
            using var clienteDoc = JsonDocument.Parse(await cliente.Content.ReadAsStringAsync().ConfigureAwait(false));
            var clienteId = clienteDoc.RootElement.GetProperty("id").GetInt32();
            var clienteRow = clienteDoc.RootElement.GetProperty("RowVersion").GetString()!;

            using var productoContent = new StringContent(
                $"{{\"strNombreProducto\":\"Factura{tag}\",\"intNumeroExistencia\":{existencia},\"decPrecio\":{precio.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}",
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
