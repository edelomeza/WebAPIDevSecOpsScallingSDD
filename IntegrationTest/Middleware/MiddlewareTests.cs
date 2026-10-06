using System.Net;
using System;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationTest.Middleware
{
    public class MiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public MiddlewareTests(WebApplicationFactory<Program> factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PingReturnsPong()
        {
            var response = await _client.GetAsync(new Uri("/ping", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("pong", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task HstsHeaderNotPresentInDevelopment()
        {
            var response = await _client.GetAsync(new Uri("/ping", UriKind.Relative));
            Assert.False(response.Headers.Contains("Strict-Transport-Security"));
        }

        [Fact]
        public async Task CorsBlocksUnknownOrigins()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/ping");
            request.Headers.Add("Origin", "https://evil.example.com");
            var response = await _client.SendAsync(request);
            Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        }
    }
}
