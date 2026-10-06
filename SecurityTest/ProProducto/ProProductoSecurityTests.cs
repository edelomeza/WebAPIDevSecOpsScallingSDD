using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecurityTest.ProProducto
{
    public class ProProductoSecurityTests
    {
        [Fact]
        public async Task AnonymousGetCollectionIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/api/v1/productos", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AnonymousGetByIdIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/api/v1/productos/1", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AnonymousPostIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var content = new StringContent(
                "{\"strNombreProducto\":\"Laptop\",\"intNumeroExistencia\":5,\"decPrecio\":99.99}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(new Uri("/api/v1/productos", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AnonymousPutIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var content = new StringContent(
                "{\"id\":1,\"strNombreProducto\":\"Laptop\",\"intNumeroExistencia\":5,\"decPrecio\":99.99,\"RowVersion\":\"AQ==\"}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync(new Uri("/api/v1/productos/1", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AnonymousDeleteIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri("/api/v1/productos/1", UriKind.Relative))
            {
                Content = new StringContent(
                    "{\"id\":1,\"RowVersion\":\"AQ==\"}",
                    Encoding.UTF8,
                    "application/json"),
            };

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AnonymousSearchIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/api/v1/productos/search?texto=Tornillo", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
