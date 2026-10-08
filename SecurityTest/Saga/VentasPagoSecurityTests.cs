using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecurityTest.Saga
{
    public class VentasPagoSecurityTests
    {
        [Fact]
        public async Task AnonymousCreateIsUnauthorizedWithoutLeaks()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var content = new StringContent(
                "{\"idVenPedido\":\"11111111-1111-1111-1111-111111111111\",\"decMonto\":10}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(new Uri("/api/v1/ventas/pago", UriKind.Relative), content);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.DoesNotContain("idVenPedido", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AnonymousGetByIdIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/api/v1/ventas/pago/1", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AnonymousGetByPedidoIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/api/v1/ventas/pago/pedido/11111111-1111-1111-1111-111111111111", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
