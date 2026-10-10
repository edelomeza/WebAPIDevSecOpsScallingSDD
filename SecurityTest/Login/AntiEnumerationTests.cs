using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SecurityTest.Login
{
    public class AntiEnumerationTests
    {
        [Fact]
        public async Task TwoUnknownUsersReturnIdenticalUnauthorizedWithinMargin()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var first = Stopwatch.StartNew();
            var unknown1 = await PostLoginAsync(client, "NadieUno", "Secreto123");
            first.Stop();
            var body1 = await unknown1.Content.ReadAsStringAsync();

            var second = Stopwatch.StartNew();
            var unknown2 = await PostLoginAsync(client, "NadieDos", "Secreto123");
            second.Stop();
            var body2 = await unknown2.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, unknown1.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, unknown2.StatusCode);
            Assert.Equal(body1, body2);
            var delta = Math.Abs((first.Elapsed - second.Elapsed).TotalMilliseconds);
            Assert.True(delta < 10000, $"Timing delta {delta}ms exceeds 10000ms margin.");
        }

        [Fact]
        public async Task BadLoginBodyContainsNoSecrets()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await PostLoginAsync(client, "Nadie", "MalaSecreta123");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("error", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MalaSecreta123", body, StringComparison.Ordinal);
            Assert.DoesNotContain("strPWD", body, StringComparison.Ordinal);
            Assert.DoesNotContain("argon2", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RepeatedUnknownFailuresLockOut()
        {
            // (04-04) El flujo emite 6 logins; eleva el límite para no colisionar con la policy Login 5/5min.
#pragma warning disable CA2000 // La factory interior se dispone con el wrapper.
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(
#pragma warning restore CA2000
                (_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:LoginPermitLimit"] = "100",
                })));
            using var client = factory.CreateClient();

            for (var i = 0; i < 5; i++)
            {
                var failure = await PostLoginAsync(client, "FantasmaBloqueo", "Mala1234");
                Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
            }

            var locked = await PostLoginAsync(client, "FantasmaBloqueo", "Mala1234");
            Assert.Equal((HttpStatusCode)423, locked.StatusCode);
        }

        private static async Task<System.Net.Http.HttpResponseMessage> PostLoginAsync(System.Net.Http.HttpClient client, string nombre, string password)
        {
            using var content = new StringContent(
                $"{{\"strNombre\":\"{nombre}\",\"strPasswordPlano\":\"{password}\"}}",
                Encoding.UTF8,
                "application/json");
            return await client.PostAsync(new Uri("/api/v1/auth/login", UriKind.Relative), content).ConfigureAwait(false);
        }
    }
}
