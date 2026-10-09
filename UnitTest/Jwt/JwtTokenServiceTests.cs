using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using WebAPIDevSecOpsScallingSDD.Services;

namespace UnitTest.Jwt
{
    public class JwtTokenServiceTests
    {
        [Fact]
        public void NullConfigurationThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new JwtTokenService(null!));
        }

        [Fact]
        public void ShortKeyThrowsInvalidOperation()
        {
            var configuration = CreateConfiguration("corta");

            Assert.Throws<InvalidOperationException>(() => new JwtTokenService(configuration));
        }

        [Fact]
        public void MissingKeyFallsBackToPlaceholder()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

            var service = new JwtTokenService(configuration);

            Assert.Equal(3, service.CreateAccessToken(7).Split('.').Length);
        }

        [Fact]
        public void CreateAccessTokenEmitsHs256WithExpectedClaims()
        {
            var service = new JwtTokenService(CreateConfiguration(new string('K', 32)));

            var token = service.CreateAccessToken(7, "Admin");
            var parts = token.Split('.');

            Assert.Equal(3, parts.Length);
            Assert.Equal("HS256", GetHeader(parts[0]).GetProperty("alg").GetString());
            var payload = GetPayload(parts[1]);
            Assert.Equal("7", payload.GetProperty("sub").GetString());
            Assert.Equal("Admin", payload.GetProperty("role").GetString());
            Assert.False(string.IsNullOrEmpty(payload.GetProperty("jti").GetString()));
            Assert.True(payload.GetProperty("exp").GetInt64() > DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        [Fact]
        public void ConsecutiveTokensDifferByJti()
        {
            var service = new JwtTokenService(CreateConfiguration(new string('K', 32)));

            Assert.NotEqual(service.CreateAccessToken(7), service.CreateAccessToken(7));
        }

        private static IConfiguration CreateConfiguration(string key)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = key,
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:Audience"] = "test-audience",
                })
                .Build();
        }

        private static JsonElement GetHeader(string part)
        {
            return JsonDocument.Parse(Encoding.UTF8.GetString(Base64UrlDecode(part))).RootElement;
        }

        private static JsonElement GetPayload(string part)
        {
            return JsonDocument.Parse(Encoding.UTF8.GetString(Base64UrlDecode(part))).RootElement;
        }

        private static byte[] Base64UrlDecode(string part)
        {
            var padded = part.Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - (padded.Length % 4)) % 4);
            return Convert.FromBase64String(padded);
        }
    }
}
