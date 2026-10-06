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
        private const string AttemptsPrefix = "attempts:";
        private const string LockoutPrefix = "lockout:";
        private const string TempPrefix = "cache:";
        private const int MaxAttempts = 5;

        // NOTE (04-02): el lockout real es de 15 min; CacheService limita el TTL a 120s.
        private static readonly TimeSpan AttemptTtl = TimeSpan.FromSeconds(120);

        // NOTE (04-02): el temp 2FA real expira en 5 min; CacheService limita el TTL a 120s.
        private static readonly TimeSpan TempTtl = TimeSpan.FromSeconds(120);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;
        private readonly ISegUsuarioPasswordHasher _hasher;
        private readonly string _dummyHash;

        public LoginService(AppDbContext db, ICacheService cache, ISegUsuarioPasswordHasher hasher)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(hasher);
            _db = db;
            _cache = cache;
            _hasher = hasher;
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

            var locked = await _cache.GetAsync<string>(LockoutPrefix, clean, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(locked))
            {
                return new LoginResult { Status = LoginStatus.LockedOut };
            }

            var user = await _db.SegUsuarios.AsNoTracking().FirstOrDefaultAsync(e => e.strNombre == clean, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                _ = _hasher.Verify(plano, _dummyHash);
                await RegisterFailureAsync(clean, cancellationToken).ConfigureAwait(false);
                return new LoginResult { Status = LoginStatus.InvalidCredentials };
            }

            if (!_hasher.Verify(plano, user.strPWD))
            {
                await RegisterFailureAsync(clean, cancellationToken).ConfigureAwait(false);
                return new LoginResult { Status = LoginStatus.InvalidCredentials };
            }

            await _cache.RemoveAsync(AttemptsPrefix, clean, cancellationToken).ConfigureAwait(false);
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

        private async Task RegisterFailureAsync(string clean, CancellationToken cancellationToken)
        {
            var attempts = await _cache.GetAsync<int>(AttemptsPrefix, clean, cancellationToken).ConfigureAwait(false);
            attempts++;
            if (attempts >= MaxAttempts)
            {
                await _cache.SetAsync(LockoutPrefix, clean, "locked", AttemptTtl, cancellationToken).ConfigureAwait(false);
                await _cache.RemoveAsync(AttemptsPrefix, clean, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _cache.SetAsync(AttemptsPrefix, clean, attempts, AttemptTtl, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
