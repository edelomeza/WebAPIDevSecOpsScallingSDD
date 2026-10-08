using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecurityTest.Saga
{
    public class VentasFacturaSecurityTests
    {
        [Fact]
        public async Task AnonymousGetByIdIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/api/v1/ventas/factura/1", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
