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

namespace IntegrationTest.VentaDetalle
{
    public class VentaDetalleControllerTests
    {
        [Fact]
        public async Task AddAndRemoveFlowRestoresStock()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            var fks = await SeedFksAsync(client, "VdFlow", existencia: 5, precio: 10m);

            var created = await PostVentaAsync(client, fks.ClienteId, fks.UsuarioId, fks.EstadoId, fks.ProductoId, "VTA-VDF001", 2);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var ventaId = createdDoc.RootElement.GetProperty("id").GetInt32();
            Assert.Equal(3, await GetExistenciaAsync(client, fks.ProductoId));

            AddOwnerHeaders(client, fks.UsuarioId);
            using var addContent = new StringContent(
                $"{{\"idProProducto\":{fks.ProductoId},\"intPiezaVenta\":1}}",
                Encoding.UTF8,
                "application/json");
            var added = await client.PostAsync(new Uri($"/api/v1/ventas/{ventaId}/detalles", UriKind.Relative), addContent);
            Assert.Equal(HttpStatusCode.Created, added.StatusCode);
            using var addedDoc = JsonDocument.Parse(await added.Content.ReadAsStringAsync());
            var detalleId = addedDoc.RootElement.GetProperty("id").GetInt32();
            var rowVersion = addedDoc.RootElement.GetProperty("RowVersion").GetString()!;
            Assert.Equal(2, await GetExistenciaAsync(client, fks.ProductoId));

            var fetched = await client.GetAsync(new Uri($"/api/v1/ventas/detalles/{detalleId}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);

            using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri($"/api/v1/ventas/detalles/{detalleId}", UriKind.Relative))
            {
                Content = new StringContent($"{{\"id\":{detalleId},\"RowVersion\":\"{rowVersion}\"}}", Encoding.UTF8, "application/json"),
            };
            var deleted = await client.SendAsync(deleteRequest);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            Assert.Equal(3, await GetExistenciaAsync(client, fks.ProductoId));

            var gone = await client.GetAsync(new Uri($"/api/v1/ventas/detalles/{detalleId}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task AddWithWrongOwnerIsForbiddenAndMissingIsNotFound()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            var fks = await SeedFksAsync(client, "VdOwn", existencia: 5, precio: 10m);
            var created = await PostVentaAsync(client, fks.ClienteId, fks.UsuarioId, fks.EstadoId, fks.ProductoId, "VTA-VDO001", 1);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var ventaId = createdDoc.RootElement.GetProperty("id").GetInt32();

            using var stranger = factory.CreateClient();
            stranger.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            stranger.DefaultRequestHeaders.Add("X-Test-UserId", "999999");
            using var strangerContent = new StringContent(
                $"{{\"idProProducto\":{fks.ProductoId},\"intPiezaVenta\":1}}",
                Encoding.UTF8,
                "application/json");
            var forbidden = await stranger.PostAsync(new Uri($"/api/v1/ventas/{ventaId}/detalles", UriKind.Relative), strangerContent);
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

            AddOwnerHeaders(client, fks.UsuarioId);
            using var missingContent = new StringContent(
                $"{{\"idProProducto\":{fks.ProductoId},\"intPiezaVenta\":1}}",
                Encoding.UTF8,
                "application/json");
            var missing = await client.PostAsync(new Uri("/api/v1/ventas/999999/detalles", UriKind.Relative), missingContent);
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task AutocompleteProductosFlow()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            var fks = await SeedFksAsync(client, "VdAuto", existencia: 5, precio: 10m);
            AddOwnerHeaders(client, fks.UsuarioId);

            var autocomplete = await client.GetAsync(new Uri("/api/v1/ventas/detalles/autocomplete-productos?texto=VentaVdAuto&maxResultados=5", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, autocomplete.StatusCode);
            var autoBody = await autocomplete.Content.ReadAsStringAsync();
            using var autoDoc = JsonDocument.Parse(autoBody);
            Assert.True(autoDoc.RootElement.GetArrayLength() <= 5);
            Assert.True(autoDoc.RootElement.GetArrayLength() >= 1);
            foreach (var item in autoDoc.RootElement.EnumerateArray())
            {
                Assert.True(item.TryGetProperty("id", out _));
                Assert.True(item.TryGetProperty("strNombreProducto", out _));
                Assert.False(item.TryGetProperty("decPrecio", out _));
                Assert.False(item.TryGetProperty("intNumeroExistencia", out _));
            }

            var noTexto = await client.GetAsync(new Uri("/api/v1/ventas/detalles/autocomplete-productos?maxResultados=5", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, noTexto.StatusCode);
            var noTextoBody = await noTexto.Content.ReadAsStringAsync();
            Assert.Contains("error", noTextoBody, StringComparison.OrdinalIgnoreCase);

            var tooLong = await client.GetAsync(new Uri($"/api/v1/ventas/detalles/autocomplete-productos?texto={new string('a', 51)}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);

            var normalizedZero = await client.GetAsync(new Uri("/api/v1/ventas/detalles/autocomplete-productos?texto=VentaVdAuto&maxResultados=0", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, normalizedZero.StatusCode);
            var normalizedOver = await client.GetAsync(new Uri("/api/v1/ventas/detalles/autocomplete-productos?texto=VentaVdAuto&maxResultados=51", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, normalizedOver.StatusCode);

            await CleanupFksAsync(client, fks);
        }

        [Fact]
        public async Task AutocompleteWithUserRoleReturnsOk()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "User");

            var response = await client.GetAsync(new Uri("/api/v1/ventas/detalles/autocomplete-productos?texto=VentaVdAuto", UriKind.Relative));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task InvalidPayloadsReturnBadRequest()
        {
            using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            var fks = await SeedFksAsync(client, "VdBad", existencia: 5, precio: 10m);
            var created = await PostVentaAsync(client, fks.ClienteId, fks.UsuarioId, fks.EstadoId, fks.ProductoId, "VTA-VDB001", 1);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var ventaId = createdDoc.RootElement.GetProperty("id").GetInt32();
            AddOwnerHeaders(client, fks.UsuarioId);

            using var zeroPiezas = new StringContent(
                $"{{\"idProProducto\":{fks.ProductoId},\"intPiezaVenta\":0}}",
                Encoding.UTF8,
                "application/json");
            var badCreate = await client.PostAsync(new Uri($"/api/v1/ventas/{ventaId}/detalles", UriKind.Relative), zeroPiezas);
            Assert.Equal(HttpStatusCode.BadRequest, badCreate.StatusCode);

            using var mismatch = new StringContent(
                "{\"id\":999,\"RowVersion\":\"AQ==\"}",
                Encoding.UTF8,
                "application/json");
            using var mismatchRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri("/api/v1/ventas/detalles/1", UriKind.Relative))
            {
                Content = mismatch,
            };
            var badDelete = await client.SendAsync(mismatchRequest);
            Assert.Equal(HttpStatusCode.BadRequest, badDelete.StatusCode);

            await CleanupFksAsync(client, fks);
        }

        private sealed record Fks(int ClienteId, string ClienteRow, int UsuarioId, string UsuarioRow, int EstadoId, int ProductoId, string ProductoRow);

        private static void AddOwnerHeaders(HttpClient client, int usuarioId)
        {
            if (!client.DefaultRequestHeaders.Contains("X-Test-Role"))
            {
                client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            }

            if (client.DefaultRequestHeaders.Contains("X-Test-UserId"))
            {
                client.DefaultRequestHeaders.Remove("X-Test-UserId");
            }

            client.DefaultRequestHeaders.Add("X-Test-UserId", usuarioId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

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
            using var content = new StringContent($"{{\"idCliCliente\":{clienteId},\"idSegUsuario\":{usuarioId},\"idVenCatEstado\":{estadoId},\"strClaveVenta\":\"{clave}\",\"Detalles\":[{{\"idProProducto\":{productoId},\"intPiezaVenta\":{piezas}}}]}}", Encoding.UTF8, "application/json");
            return await client.PostAsync(new Uri("/api/v1/ventas", UriKind.Relative), content).ConfigureAwait(false);
        }

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
