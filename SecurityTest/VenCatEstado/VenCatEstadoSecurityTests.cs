using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecurityTest.VenCatEstado
{
    public class VenCatEstadoSecurityTests
    {
        [Fact]
        public async Task AnonymousGetCollectionIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/api/v1/estados-venta", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AnonymousGetByIdIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/api/v1/estados-venta/1", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AnonymousPostIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var content = new StringContent(
                "{\"strValor\":\"Vigente\"}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(new Uri("/api/v1/estados-venta", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AnonymousPutIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var content = new StringContent(
                "{\"id\":1,\"strValor\":\"Vigente\"}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync(new Uri("/api/v1/estados-venta/1", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AnonymousDeleteIsUnauthorized()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri("/api/v1/estados-venta/1", UriKind.Relative))
            {
                Content = new StringContent(
                    "{\"id\":1}",
                    Encoding.UTF8,
                    "application/json"),
            };

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
