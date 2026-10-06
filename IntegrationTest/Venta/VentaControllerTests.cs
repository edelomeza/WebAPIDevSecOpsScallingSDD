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

namespace IntegrationTest.Venta
{
    public class VentaControllerTests
    {
        [Fact]
        public async Task CreateFlowDiscountsStockAndGetById()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "Flow", existencia: 5, precio: 10.5m);

            var created = await PostVentaAsync(client, fks.ClienteId, fks.UsuarioId, fks.EstadoId, fks.ProductoId, "VTA-FLOW01", 2);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var ventaId = createdDoc.RootElement.GetProperty("id").GetInt32();
            var detalles = createdDoc.RootElement.GetProperty("Detalles");
            Assert.Equal(1, detalles.GetArrayLength());
            Assert.Equal(21m, detalles[0].GetProperty("decTotalVenta").GetDecimal());

            var fetched = await client.GetAsync(new Uri($"/api/v1/ventas/{ventaId}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);

            var stock = await GetExistenciaAsync(client, fks.ProductoId);
            Assert.Equal(3, stock);

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task UnknownFkReturnsUnprocessable()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "Unproc", existencia: 5, precio: 10m);

            var badCliente = await PostVentaAsync(client, 999999, fks.UsuarioId, fks.EstadoId, fks.ProductoId, "VTA-UNP001", 1);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, badCliente.StatusCode);
            var body = await badCliente.Content.ReadAsStringAsync();
            Assert.Contains("error", body, StringComparison.OrdinalIgnoreCase);

            var badProducto = await PostVentaAsync(client, fks.ClienteId, fks.UsuarioId, fks.EstadoId, 999999, "VTA-UNP002", 1);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, badProducto.StatusCode);

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task InsufficientStockReturnsConflict()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "Stock", existencia: 1, precio: 10m);

            var response = await PostVentaAsync(client, fks.ClienteId, fks.UsuarioId, fks.EstadoId, fks.ProductoId, "VTA-STK001", 2);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            Assert.Equal(1, await GetExistenciaAsync(client, fks.ProductoId));

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task InvalidPayloadsReturnBadRequest()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var fks = await SeedFksAsync(client, "Bad", existencia: 5, precio: 10m);

            var zeroId = await PostVentaAsync(client, 0, fks.UsuarioId, fks.EstadoId, fks.ProductoId, "VTA-BAD001", 1);
            Assert.Equal(HttpStatusCode.BadRequest, zeroId.StatusCode);

            var emptyDetalles = await PostVentaRawAsync(client, $"{{\"idCliCliente\":{fks.ClienteId},\"idSegUsuario\":{fks.UsuarioId},\"idVenCatEstado\":{fks.EstadoId},\"strClaveVenta\":\"VTA-BAD002\",\"Detalles\":[]}}");
            Assert.Equal(HttpStatusCode.BadRequest, emptyDetalles.StatusCode);

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task GetMissingReturnsNotFound()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            var missing = await client.GetAsync(new Uri("/api/v1/ventas/999999", UriKind.Relative));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }

        private sealed record Fks(int ClienteId, string ClienteRow, int UsuarioId, string UsuarioRow, int EstadoId, int ProductoId, string ProductoRow);

        private static async Task<Fks> SeedFksAsync(HttpClient client, string tag, int existencia, decimal precio)
        {
            using var clienteContent = new StringContent(
                $"{{\"strNombreCliente\":\"Venta{tag}\",\"strCorreoElectronico\":\"venta{tag}@test.local\",\"strNumeroTelefono\":\"5550000001\"}}",
                Encoding.UTF8,
                "application/json");
            var cliente = await client.PostAsync(new Uri("/api/v1/clientes", UriKind.Relative), clienteContent).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, cliente.StatusCode);
            using var clienteDoc = JsonDocument.Parse(await cliente.Content.ReadAsStringAsync().ConfigureAwait(false));
            var clienteId = clienteDoc.RootElement.GetProperty("id").GetInt32();
            var clienteRow = clienteDoc.RootElement.GetProperty("RowVersion").GetString()!;

            using var usuarioContent = new StringContent(
                $"{{\"strNombre\":\"Venta{tag}\",\"strCorreoElectronico\":\"venta{tag}@test.local\",\"strPasswordPlano\":\"Secreto123\"}}",
                Encoding.UTF8,
                "application/json");
            var usuario = await client.PostAsync(new Uri("/api/v1/usuarios", UriKind.Relative), usuarioContent).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, usuario.StatusCode);
            using var usuarioDoc = JsonDocument.Parse(await usuario.Content.ReadAsStringAsync().ConfigureAwait(false));
            var usuarioId = usuarioDoc.RootElement.GetProperty("id").GetInt32();
            var usuarioRow = usuarioDoc.RootElement.GetProperty("RowVersion").GetString()!;

            using var estadoContent = new StringContent(
                $"{{\"strValor\":\"Venta{tag}\",\"strDescripcion\":\"Estado {tag}\"}}",
                Encoding.UTF8,
                "application/json");
            var estado = await client.PostAsync(new Uri("/api/v1/estados-venta", UriKind.Relative), estadoContent).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, estado.StatusCode);
            using var estadoDoc = JsonDocument.Parse(await estado.Content.ReadAsStringAsync().ConfigureAwait(false));
            var estadoId = estadoDoc.RootElement.GetProperty("id").GetInt32();

            using var productoContent = new StringContent(
                $"{{\"strNombreProducto\":\"Venta{tag}\",\"intNumeroExistencia\":{existencia},\"decPrecio\":{precio.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}",
                Encoding.UTF8,
                "application/json");
            var producto = await client.PostAsync(new Uri("/api/v1/productos", UriKind.Relative), productoContent).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, producto.StatusCode);
            using var productoDoc = JsonDocument.Parse(await producto.Content.ReadAsStringAsync().ConfigureAwait(false));
            var productoId = productoDoc.RootElement.GetProperty("id").GetInt32();
            var productoRow = productoDoc.RootElement.GetProperty("RowVersion").GetString()!;

            return new Fks(clienteId, clienteRow, usuarioId, usuarioRow, estadoId, productoId, productoRow);
        }

        private static async Task CleanupFksAsync(HttpClient client, Fks fks)
        {
            await SendDeleteAsync(client, $"/api/v1/productos/{fks.ProductoId}", $"{{\"id\":{fks.ProductoId},\"RowVersion\":\"{fks.ProductoRow}\"}}").ConfigureAwait(false);
            await SendDeleteAsync(client, $"/api/v1/estados-venta/{fks.EstadoId}", $"{{\"id\":{fks.EstadoId}}}").ConfigureAwait(false);
            await SendDeleteAsync(client, $"/api/v1/usuarios/{fks.UsuarioId}", $"{{\"id\":{fks.UsuarioId},\"RowVersion\":\"{fks.UsuarioRow}\"}}").ConfigureAwait(false);
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

        private static async Task<HttpResponseMessage> PostVentaAsync(HttpClient client, int clienteId, int usuarioId, int estadoId, int productoId, string clave, int piezas)
        {
            return await PostVentaRawAsync(client, $"{{\"idCliCliente\":{clienteId},\"idSegUsuario\":{usuarioId},\"idVenCatEstado\":{estadoId},\"strClaveVenta\":\"{clave}\",\"Detalles\":[{{\"idProProducto\":{productoId},\"intPiezaVenta\":{piezas}}}]}}").ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> PostVentaRawAsync(HttpClient client, string json)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await client.PostAsync(new Uri("/api/v1/ventas", UriKind.Relative), content).ConfigureAwait(false);
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
