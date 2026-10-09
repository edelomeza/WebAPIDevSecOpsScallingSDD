using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ContractTest.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using WebAPIDevSecOpsScallingSDD.Services;

namespace ContractTest
{
    public class FixtureCaptureTests
    {
        private const string CaptureEnv = "CONTRACT_CAPTURE";

        [Fact]
        public async Task CaptureFlowReturnsExpectedStatusCodes()
        {
#pragma warning disable CA2000 // La factory interna se libera con el wrapper.
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Staging").ConfigureTestServices(services =>
#pragma warning restore CA2000
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, ContractAuthHandler>("Test", _ => { });
            }));
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            bool capture = string.Equals(Environment.GetEnvironmentVariable(CaptureEnv), "1", StringComparison.Ordinal);

            Save("ping.json", await GetAsync(client, "/api/v1/ping", HttpStatusCode.OK), capture);

            string usuario = await PostAsync(client, "/api/v1/usuarios", "{\"strNombre\":\"PactUser\",\"strCorreoElectronico\":\"pact@test.local\",\"strPasswordPlano\":\"Pact12345!\"}", HttpStatusCode.Created);
            Save("segusuario.json", usuario, capture);
            int usuarioId = GetInt(usuario, "id");

            string cliente = await PostAsync(client, "/api/v1/clientes", "{\"strNombreCliente\":\"PactCliente\",\"strCorreoElectronico\":\"pactc@test.local\",\"strNumeroTelefono\":\"5550000001\"}", HttpStatusCode.Created);
            Save("cliente.json", cliente, capture);
            int clienteId = GetInt(cliente, "id");

            string producto = await PostAsync(client, "/api/v1/productos", "{\"strNombreProducto\":\"PactProducto\",\"intNumeroExistencia\":10,\"decPrecio\":99.50}", HttpStatusCode.Created);
            Save("producto.json", producto, capture);
            int productoId = GetInt(producto, "id");

            string estado = await PostAsync(client, "/api/v1/estados-venta", "{\"strValor\":\"PactEstado\"}", HttpStatusCode.Created);
            Save("estado-venta.json", estado, capture);
            int estadoId = GetInt(estado, "id");

            Save("clientes-paged.json", await GetAsync(client, "/api/v1/clientes?page=1&pageSize=20", HttpStatusCode.OK), capture);
            Save("clientes-autocomplete.json", await GetAsync(client, "/api/v1/clientes/autocomplete?texto=Pact", HttpStatusCode.OK), capture);

            string login = await PostAsync(client, "/api/v1/auth/login", "{\"strNombre\":\"PactUser\",\"strPasswordPlano\":\"Pact12345!\"}", HttpStatusCode.OK);
            Save("login-response.json", login, capture);
            string refreshToken = await CreateRefreshPairAsync(factory, usuarioId);

            string refresh = await PostAsync(client, "/api/v1/auth/refresh", "{\"RefreshToken\":\"" + refreshToken + "\"}", HttpStatusCode.OK);
            Save("refresh-response.json", refresh, capture);

            string pedido = await PostAsync(client, "/api/v1/ventas/pedido", "{\"idCliCliente\":" + clienteId + ",\"Detalles\":[{\"idProProducto\":" + productoId + ",\"intCantidad\":2}]}", HttpStatusCode.Created);
            Save("pedido.json", pedido, capture);
            string pedidoId = GetString(pedido, "id");

            string pago = await PostAsync(client, "/api/v1/ventas/pago", "{\"idVenPedido\":\"" + pedidoId + "\",\"decMonto\":199.00,\"strMetodoPago\":\"Tarjeta\",\"strIdTransaccion\":\"TX-PACT-1\"}", HttpStatusCode.Created);
            Save("pago.json", pago, capture);

            string venta = await PostAsync(client, "/api/v1/ventas", "{\"idCliCliente\":" + clienteId + ",\"idSegUsuario\":" + usuarioId + ",\"idVenCatEstado\":" + estadoId + ",\"strClaveVenta\":\"PACT-001\",\"Detalles\":[{\"idProProducto\":" + productoId + ",\"intPiezaVenta\":1}]}", HttpStatusCode.Created);
            Save("venta.json", venta, capture);

            Save("dashboard.json", await GetAsync(client, "/api/v1/ventas/dashboard", HttpStatusCode.OK), capture);
            Save("error-404.json", await GetAsync(client, "/api/v1/clientes/999999", HttpStatusCode.NotFound), capture);
        }

        private static async Task<string> GetAsync(HttpClient client, string url, HttpStatusCode expected)
        {
            using var response = await client.GetAsync(new Uri(url, UriKind.Relative)).ConfigureAwait(false);
            Assert.Equal(expected, response.StatusCode);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static async Task<string> PostAsync(HttpClient client, string url, string json, HttpStatusCode expected)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(new Uri(url, UriKind.Relative), content).ConfigureAwait(false);
            Assert.Equal(expected, response.StatusCode);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static async Task<string> CreateRefreshPairAsync(WebApplicationFactory<Program> factory, int usuarioId)
        {
            using var scope = factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
            var pair = await service.CreateAsync(usuarioId).ConfigureAwait(false);
            return pair.RefreshToken;
        }

        private static int GetInt(string body, string property)
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty(property).GetInt32();
        }

        private static string GetString(string body, string property)
        {
            using var doc = JsonDocument.Parse(body);
            var value = doc.RootElement.GetProperty(property).GetString();
            Assert.False(string.IsNullOrEmpty(value));
            return value!;
        }

        private static void Save(string name, string body, bool capture)
        {
            if (!capture)
            {
                return;
            }

            File.WriteAllText(Path.Combine(FixturePaths.Directory, name), body + Environment.NewLine);
        }
    }
}
