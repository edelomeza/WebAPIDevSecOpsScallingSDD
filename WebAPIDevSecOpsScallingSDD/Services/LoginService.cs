using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public enum LoginStatus
    {
        Authenticated,
        InvalidCredentials,
        LockedOut,
        RequiresTwoFactor,
    }

    public sealed class LoginResult
    {
        public LoginStatus Status { get; set; }

        public string? Token { get; set; }

        public string? TempCode { get; set; }
    }

    public interface ILoginService
    {
        Task<LoginResult> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default);
    }

    public sealed class LoginService : ILoginService
    {
        private const string TempPrefix = "cache:";

        // NOTE (04-02): rehash transparente diferido (requiere diseno de escritura con RowVersion; ver spec.md Limites).

        // NOTE (04-02): el temp 2FA real expira en 5 min; CacheService limita el TTL a 120s.
        private static readonly TimeSpan TempTtl = TimeSpan.FromSeconds(120);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;
        private readonly ISegUsuarioPasswordHasher _hasher;
        private readonly ILoginLockoutStore _lockout;
        private readonly string _dummyHash;

        public LoginService(AppDbContext db, ICacheService cache, ISegUsuarioPasswordHasher hasher, ILoginLockoutStore lockout)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(hasher);
            ArgumentNullException.ThrowIfNull(lockout);
            _db = db;
            _cache = cache;
            _hasher = hasher;
            _lockout = lockout;
            _dummyHash = hasher.Hash("login-anti-enumeration-dummy");
        }

        public async Task<LoginResult> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var clean = (request.strNombre ?? string.Empty).Trim();
            var plano = request.strPasswordPlano ?? string.Empty;
            if (clean.Length == 0 || plano.Length == 0)
            {
                return new LoginResult { Status = LoginStatus.InvalidCredentials };
            }

            // Bloqueo persistente 15 min (04-02): SegBloqueo vía ILoginLockoutStore, fuera de la caché efímera.
            if (await _lockout.IsLockedAsync(clean, cancellationToken).ConfigureAwait(false))
            {
                return new LoginResult { Status = LoginStatus.LockedOut };
            }

            var user = await _db.SegUsuarios.AsNoTracking().FirstOrDefaultAsync(e => e.strNombre == clean, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                _ = _hasher.Verify(plano, _dummyHash);
                await _lockout.RecordFailureAsync(clean, cancellationToken).ConfigureAwait(false);
                return new LoginResult { Status = LoginStatus.InvalidCredentials };
            }

            if (!_hasher.Verify(plano, user.strPWD))
            {
                await _lockout.RecordFailureAsync(clean, cancellationToken).ConfigureAwait(false);
                return new LoginResult { Status = LoginStatus.InvalidCredentials };
            }

            await _lockout.ResetAsync(clean, cancellationToken).ConfigureAwait(false);
            if (user.bln2FAHabilitado)
            {
                // NOTE (04-01): temp opaco temporal; reemplazar por JWT con claim 2fa_temp.
                // Hex (no Base64) para no tropezar con los patrones prohibidos de CacheService.
                var temp = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                await _cache.SetAsync(TempPrefix, $"login2fa:{temp}", clean, TempTtl, cancellationToken).ConfigureAwait(false);
                return new LoginResult { Status = LoginStatus.RequiresTwoFactor, TempCode = temp };
            }

            // NOTE (04-01): token opaco temporal; reemplazar por JWT HS256 real.
            return new LoginResult { Status = LoginStatus.Authenticated, Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) };
        }
    }
}
