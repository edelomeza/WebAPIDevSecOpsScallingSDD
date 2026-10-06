using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public enum Login2FaStatus
    {
        Authenticated,
        InvalidCredentials,
        LockedOut,
    }

    public sealed class Login2FaResult
    {
        public Login2FaStatus Status { get; set; }

        public string? Token { get; set; }
    }

    public interface ILogin2FaService
    {
        Task<Login2FaResult> VerifyAsync(Login2FaVerifyRequest request, CancellationToken cancellationToken = default);
    }

    public sealed class Login2FaService : ILogin2FaService
    {
        private const string AttemptsPrefix = "attempts:";
        private const string LockoutPrefix = "lockout:";
        private const string TempPrefix = "cache:";
        private const int MaxAttempts = 5;

        // NOTE (04-02): el temp real expira en 5 min; CacheService limita el TTL a 120s.
        private static readonly TimeSpan AttemptTtl = TimeSpan.FromSeconds(120);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;
        private readonly ITotpService _totp;

        public Login2FaService(AppDbContext db, ICacheService cache, ITotpService totp)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(totp);
            _db = db;
            _cache = cache;
            _totp = totp;
        }

        public async Task<Login2FaResult> VerifyAsync(Login2FaVerifyRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.TempToken is null || request.TotpCode is null)
            {
                return new Login2FaResult { Status = Login2FaStatus.InvalidCredentials };
            }

            var temp = request.TempToken.Trim();
            var code = request.TotpCode.Trim();
            if (temp.Length == 0 || code.Length == 0 || !IsHexToken(temp))
            {
                return new Login2FaResult { Status = Login2FaStatus.InvalidCredentials };
            }

            var nombre = await _cache.GetAsync<string>(TempPrefix, $"login2fa:{temp}", cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(nombre))
            {
                _ = _totp.Verify("login2fa-dummy", code);
                return new Login2FaResult { Status = Login2FaStatus.InvalidCredentials };
            }

            var locked = await _cache.GetAsync<string>(LockoutPrefix, nombre, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(locked))
            {
                return new Login2FaResult { Status = Login2FaStatus.LockedOut };
            }

            var user = await _db.SegUsuarios.AsNoTracking().FirstOrDefaultAsync(e => e.strNombre == nombre, cancellationToken).ConfigureAwait(false);
            if (user is null || !user.bln2FAHabilitado || string.IsNullOrEmpty(user.str2FASecreto))
            {
                _ = _totp.Verify("login2fa-dummy", code);
                await RegisterFailureAsync(nombre, cancellationToken).ConfigureAwait(false);
                return new Login2FaResult { Status = Login2FaStatus.InvalidCredentials };
            }

            if (!_totp.Verify(user.str2FASecreto, code))
            {
                await RegisterFailureAsync(nombre, cancellationToken).ConfigureAwait(false);
                return new Login2FaResult { Status = Login2FaStatus.InvalidCredentials };
            }

            await _cache.RemoveAsync(TempPrefix, $"login2fa:{temp}", cancellationToken).ConfigureAwait(false);
            await _cache.RemoveAsync(AttemptsPrefix, nombre, cancellationToken).ConfigureAwait(false);
            // NOTE (04-01): token opaco temporal; reemplazar por JWT HS256 real.
            return new Login2FaResult { Status = Login2FaStatus.Authenticated, Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) };
        }

        private static bool IsHexToken(string value)
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
