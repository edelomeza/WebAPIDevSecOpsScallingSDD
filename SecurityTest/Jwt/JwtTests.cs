using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAPIDevSecOpsScallingSDD.Services;

namespace SecurityTest.Jwt
{
    public class JwtTests
    {
        [Fact]
        public async Task AlgNoneIsRejected()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            var unsigned = CraftUnsigned("{\"alg\":\"none\",\"typ\":\"JWT\"}", Payload(7, DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds(), factory));
            var bearer = unsigned + ".";

            using var content = new StringContent("{\"RefreshToken\":\"" + new string('D', 64) + "\"}", Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/auth/logout", UriKind.Relative));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            request.Content = content;

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TamperedSignatureIsRejected()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            var valid = await CreateAccessTokenAsync(factory, 7);
            var tampered = string.Concat(valid.AsSpan(0, valid.Length - 1), valid.EndsWith('A') ? "B" : "A");

            var response = await PostLogoutAsync(client, tampered, new string('E', 64));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ExpiredTokenIsRejected()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            var expired = CraftSigned(factory, 7, DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds());

            var response = await PostLogoutAsync(client, expired, new string('F', 64));
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.DoesNotContain(expired, body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ReusedRefreshReturnsUnauthorizedWithoutEcho()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            var pair = await CreatePairAsync(factory, 7);

            var first = await PostRefreshAsync(client, pair.RefreshToken);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var reuse = await PostRefreshAsync(client, pair.RefreshToken);
            var body = await reuse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
            Assert.DoesNotContain(pair.RefreshToken, body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task LogoutBlacklistsJtiAndBearerReplayFails()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            var pair = await CreatePairAsync(factory, 7);

            var logout = await PostLogoutAsync(client, pair.Token, pair.RefreshToken);
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

            var replay = await PostLogoutAsync(client, pair.Token, pair.RefreshToken);
            Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

            var refresh = await PostRefreshAsync(client, pair.RefreshToken);
            Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        }

        private static async Task<RefreshPair> CreatePairAsync(WebApplicationFactory<Program> factory, int userId)
        {
            using var scope = factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
            return await service.CreateAsync(userId).ConfigureAwait(false);
        }

        private static async Task<string> CreateAccessTokenAsync(WebApplicationFactory<Program> factory, int userId)
        {
            using var scope = factory.Services.CreateScope();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            return await Task.FromResult(jwt.CreateAccessToken(userId)).ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> PostRefreshAsync(HttpClient client, string refreshToken)
        {
            using var content = new StringContent("{\"RefreshToken\":\"" + refreshToken + "\"}", Encoding.UTF8, "application/json");
            return await client.PostAsync(new Uri("/api/v1/auth/refresh", UriKind.Relative), content).ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> PostLogoutAsync(HttpClient client, string bearer, string refreshToken)
        {
            using var content = new StringContent("{\"RefreshToken\":\"" + refreshToken + "\"}", Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/auth/logout", UriKind.Relative));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            request.Content = content;
            return await client.SendAsync(request).ConfigureAwait(false);
        }

        private static string Payload(int userId, long exp, WebApplicationFactory<Program> factory)
        {
            var configuration = factory.Services.GetRequiredService<IConfiguration>();
            var issuer = configuration["Jwt:Issuer"];
            if (string.IsNullOrWhiteSpace(issuer))
            {
                issuer = JwtTokenService.FallbackIssuer;
            }

            var audience = configuration["Jwt:Audience"];
            if (string.IsNullOrWhiteSpace(audience))
            {
                audience = JwtTokenService.FallbackAudience;
            }

            return "{\"sub\":\"" + userId + "\",\"jti\":\"abc123\",\"role\":\"User\",\"iss\":\"" + issuer + "\",\"aud\":\"" + audience + "\",\"exp\":" + exp + "}";
        }

        private static string CraftUnsigned(string headerJson, string payloadJson)
        {
            return Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson)) + "." + Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        }

        private static string CraftSigned(WebApplicationFactory<Program> factory, int userId, long exp)
        {
            var configuration = factory.Services.GetRequiredService<IConfiguration>();
            var rawKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(rawKey))
            {
                rawKey = JwtTokenService.FallbackKey;
            }

            var unsigned = CraftUnsigned("{\"alg\":\"HS256\",\"typ\":\"JWT\"}", Payload(userId, exp, factory));
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(rawKey));
            return unsigned + "." + Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsigned)));
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
