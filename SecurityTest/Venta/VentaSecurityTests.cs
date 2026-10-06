using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecurityTest.Venta
{
    public class VentaSecurityTests
    {
        [Fact]
        public async Task AnonymousCreateIsUnauthorizedWithoutLeaks()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var content = new StringContent(
                "{\"idCliCliente\":1,\"idSegUsuario\":1,\"idVenCatEstado\":1,\"strClaveVenta\":\"VTA-000001\",\"Detalles\":[{\"idProProducto\":1,\"intPiezaVenta\":1}]}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(new Uri("/api/v1/ventas", UriKind.Relative), content);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.DoesNotContain("VTA-000001", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AnonymousGetByIdIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/api/v1/ventas/1", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
