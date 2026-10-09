using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Models;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public enum RefreshStatus
    {
        Rotated,
        Invalid,
    }

    public sealed class RefreshResult
    {
        public RefreshStatus Status { get; set; }

        public string? Token { get; set; }

        public string? RefreshToken { get; set; }
    }

    public sealed class RefreshPair
    {
        public string Token { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;
    }

    public interface IRefreshTokenService
    {
        Task<RefreshPair> CreateAsync(int userId, CancellationToken cancellationToken = default);

        Task<RefreshResult> RotateAsync(string? refreshPlano, CancellationToken cancellationToken = default);

        Task<bool> RevokeAsync(string? refreshPlano, CancellationToken cancellationToken = default);

        Task LogoutAsync(string? refreshPlano, string? jtiClaim, CancellationToken cancellationToken = default);
    }

    public sealed class RefreshTokenService : IRefreshTokenService
    {
        private const string BlacklistPrefix = "blacklist:";

        // NOTE (04-02): la revocación real vive más que 120s; CacheService limita el TTL a 120s.
        private static readonly TimeSpan BlacklistTtl = TimeSpan.FromSeconds(120);

        // (04-01) Refresh 7d vigente; el TTL definitivo de revocación vive en 04-02.
        private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;
        private readonly IJwtTokenService _jwt;

        public RefreshTokenService(AppDbContext db, ICacheService cache, IJwtTokenService jwt)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(jwt);
            _db = db;
            _cache = cache;
            _jwt = jwt;
        }

        public async Task<RefreshPair> CreateAsync(int userId, CancellationToken cancellationToken = default)
        {
            var access = _jwt.CreateAccessToken(userId);
            var refreshPlano = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var now = DateTime.UtcNow;
            _db.SegRefreshTokens.Add(new SegRefreshToken
            {
                idSegUsuario = userId,
                strTokenHash = ComputeHash(refreshPlano),
                dteCreatedAt = now,
                dteExpiresAt = now.Add(RefreshLifetime),
            });
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new RefreshPair { Token = access, RefreshToken = refreshPlano };
        }

        public async Task<RefreshResult> RotateAsync(string? refreshPlano, CancellationToken cancellationToken = default)
        {
            var clean = (refreshPlano ?? string.Empty).Trim();
            if (clean.Length == 0 || !IsHex(clean))
            {
                return new RefreshResult { Status = RefreshStatus.Invalid };
            }

            var hash = ComputeHash(clean);
            var current = await _db.SegRefreshTokens
                .FirstOrDefaultAsync(e => e.strTokenHash == hash, cancellationToken).ConfigureAwait(false);
            if (current is null || current.dteRevokedAt.HasValue || current.dteExpiresAt <= DateTime.UtcNow)
            {
                return new RefreshResult { Status = RefreshStatus.Invalid };
            }

            var newAccess = _jwt.CreateAccessToken(current.idSegUsuario);
            var newRefreshPlano = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var newHash = ComputeHash(newRefreshPlano);
            var now = DateTime.UtcNow;
            current.dteRevokedAt = now;
            current.strReplacedByTokenHash = newHash;
            _db.SegRefreshTokens.Add(new SegRefreshToken
            {
                idSegUsuario = current.idSegUsuario,
                strTokenHash = newHash,
                dteCreatedAt = now,
                dteExpiresAt = now.Add(RefreshLifetime),
            });
            try
            {
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return new RefreshResult { Status = RefreshStatus.Invalid };
            }

            return new RefreshResult { Status = RefreshStatus.Rotated, Token = newAccess, RefreshToken = newRefreshPlano };
        }

        public async Task<bool> RevokeAsync(string? refreshPlano, CancellationToken cancellationToken = default)
        {
            var clean = (refreshPlano ?? string.Empty).Trim();
            if (clean.Length == 0 || !IsHex(clean))
            {
                return false;
            }

            var hash = ComputeHash(clean);
            var current = await _db.SegRefreshTokens
                .FirstOrDefaultAsync(e => e.strTokenHash == hash, cancellationToken).ConfigureAwait(false);
            if (current is null || current.dteRevokedAt.HasValue)
            {
                return false;
            }

            current.dteRevokedAt = DateTime.UtcNow;
            try
            {
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }

            return true;
        }

        public async Task LogoutAsync(string? refreshPlano, string? jtiClaim, CancellationToken cancellationToken = default)
        {
            await RevokeAsync(refreshPlano, cancellationToken).ConfigureAwait(false);
            var jti = (jtiClaim ?? string.Empty).Trim();
            if (jti.Length == 0)
            {
                // (04-01) Fallback defensivo: con JWT el jti siempre viene del claim; sin claim se deriva del hash.
                var clean = (refreshPlano ?? string.Empty).Trim();
                if (clean.Length == 0 || !IsHex(clean))
                {
                    return;
                }

                jti = ComputeHash(clean);
            }

            await _cache.SetAsync(BlacklistPrefix, jti, "revoked", BlacklistTtl, cancellationToken).ConfigureAwait(false);
        }

        internal static string ComputeHash(string plano)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plano)));
        }

        private static bool IsHex(string value)
        {
            foreach (var c in value)
            {
                if (!Uri.IsHexDigit(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
