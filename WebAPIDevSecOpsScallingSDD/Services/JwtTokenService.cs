using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface IJwtTokenService
    {
        string CreateAccessToken(int userId, string role = "User");
    }

    public sealed class JwtTokenService : IJwtTokenService
    {
        public const string FallbackKey = "PLACEHOLDER_HS256_KEY_MIN_32_BYTES";

        public const string FallbackIssuer = "placeholder-issuer";

        public const string FallbackAudience = "placeholder-audience";

        private static readonly TimeSpan AccessLifetime = TimeSpan.FromMinutes(15);

        private readonly byte[] _key;

        private readonly string _issuer;

        private readonly string _audience;

        public JwtTokenService(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var rawKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(rawKey))
            {
                rawKey = FallbackKey;
            }

            _key = Encoding.UTF8.GetBytes(rawKey);
            if (_key.Length < 32)
            {
                throw new InvalidOperationException("Jwt:Key must be at least 32 bytes for HS256.");
            }

            var rawIssuer = configuration["Jwt:Issuer"];
            _issuer = string.IsNullOrWhiteSpace(rawIssuer) ? FallbackIssuer : rawIssuer;

            var rawAudience = configuration["Jwt:Audience"];
            _audience = string.IsNullOrWhiteSpace(rawAudience) ? FallbackAudience : rawAudience;
        }

        public string CreateAccessToken(int userId, string role = "User")
        {
            // Literales cortos: el mapeo inbound de JwtBearer los eleva a NameIdentifier/Role (ver Logout/TwoFactor/AdminPolicy).
            var claims = new List<Claim>
            {
                new("sub", userId.ToString(CultureInfo.InvariantCulture)),
                new("jti", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)),
                new("role", role ?? "User"),
            };
            var credentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(_issuer, _audience, claims, expires: DateTime.UtcNow.Add(AccessLifetime), signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
