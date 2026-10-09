using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public enum TwoFactorStatus
    {
        Enabled,
        InvalidCode,
        LockedOut,
    }

    public sealed class TwoFactorResult
    {
        public TwoFactorStatus Status { get; set; }
    }

    public interface ITwoFactorService
    {
        Task<TwoFactorSetupResponse?> SetupAsync(string userId, CancellationToken cancellationToken = default);

        Task<TwoFactorResult> VerifyAsync(string userId, TwoFactorVerifyRequest request, CancellationToken cancellationToken = default);
    }

    public sealed class TwoFactorService : ITwoFactorService
    {
        private const string AttemptsPrefix = "attempts:";
        private const string LockoutPrefix = "lockout:";
        private const int MaxAttempts = 5;

        // NOTE (04-02): el lockout real es de 15 min; CacheService limita el TTL a 120s.
        private static readonly TimeSpan AttemptTtl = TimeSpan.FromSeconds(120);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;
        private readonly ITotpService _totp;
        private readonly ITotpProvisioner _provisioner;
        private readonly ITwoFactorSecretProtector _protector;

        public TwoFactorService(AppDbContext db, ICacheService cache, ITotpService totp, ITotpProvisioner provisioner, ITwoFactorSecretProtector protector)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(totp);
            ArgumentNullException.ThrowIfNull(provisioner);
            ArgumentNullException.ThrowIfNull(protector);
            _db = db;
            _cache = cache;
            _totp = totp;
            _provisioner = provisioner;
            _protector = protector;
        }

        public async Task<TwoFactorSetupResponse?> SetupAsync(string userId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(userId);
            var cleanUserId = userId.Trim();
            if (cleanUserId.Length == 0)
            {
                throw new ArgumentException("User id is required.", nameof(userId));
            }

            if (!int.TryParse(cleanUserId, out var id))
            {
                return null;
            }

            var user = await _db.SegUsuarios.FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                return null;
            }

            var secret = _provisioner.GenerateSecret();
            var uri = _provisioner.BuildOtpAuthUri(secret, user.strNombre, "WebAPIDevSecOpsScallingSDD");
            user.str2FASecreto = _protector.Protect(secret);
            user.bln2FAHabilitado = false;
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new TwoFactorSetupResponse { Secret = secret, OtpAuthUri = uri.ToString() };
        }

        public async Task<TwoFactorResult> VerifyAsync(string userId, TwoFactorVerifyRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(userId);
            ArgumentNullException.ThrowIfNull(request);
            var cleanUserId = userId.Trim();
            if (cleanUserId.Length == 0)
            {
                throw new ArgumentException("User id is required.", nameof(userId));
            }

            if (request.TotpCode is null)
            {
                return new TwoFactorResult { Status = TwoFactorStatus.InvalidCode };
            }

            var code = request.TotpCode.Trim();
            if (code.Length == 0)
            {
                return new TwoFactorResult { Status = TwoFactorStatus.InvalidCode };
            }

            var rateKey = $"2fa:{cleanUserId}";
            var locked = await _cache.GetAsync<string>(LockoutPrefix, rateKey, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(locked))
            {
                return new TwoFactorResult { Status = TwoFactorStatus.LockedOut };
            }

            if (!int.TryParse(cleanUserId, out var id))
            {
                _ = _totp.Verify("twofactor-dummy", code);
                await RegisterFailureAsync(rateKey, cancellationToken).ConfigureAwait(false);
                return new TwoFactorResult { Status = TwoFactorStatus.InvalidCode };
            }

            var user = await _db.SegUsuarios.FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (user is null || string.IsNullOrEmpty(user.str2FASecreto))
            {
                _ = _totp.Verify("twofactor-dummy", code);
                await RegisterFailureAsync(rateKey, cancellationToken).ConfigureAwait(false);
                return new TwoFactorResult { Status = TwoFactorStatus.InvalidCode };
            }

            string rawSecret;
            try
            {
                rawSecret = _protector.Unprotect(user.str2FASecreto);
            }
            catch (CryptographicException)
            {
                _ = _totp.Verify("twofactor-dummy", code);
                await RegisterFailureAsync(rateKey, cancellationToken).ConfigureAwait(false);
                return new TwoFactorResult { Status = TwoFactorStatus.InvalidCode };
            }
            catch (ArgumentException)
            {
                _ = _totp.Verify("twofactor-dummy", code);
                await RegisterFailureAsync(rateKey, cancellationToken).ConfigureAwait(false);
                return new TwoFactorResult { Status = TwoFactorStatus.InvalidCode };
            }

            if (!_totp.Verify(rawSecret, code))
            {
                await RegisterFailureAsync(rateKey, cancellationToken).ConfigureAwait(false);
                return new TwoFactorResult { Status = TwoFactorStatus.InvalidCode };
            }

            user.bln2FAHabilitado = true;
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _cache.RemoveAsync(AttemptsPrefix, rateKey, cancellationToken).ConfigureAwait(false);
            return new TwoFactorResult { Status = TwoFactorStatus.Enabled };
        }

        private async Task RegisterFailureAsync(string rateKey, CancellationToken cancellationToken)
        {
            var attempts = await _cache.GetAsync<int>(AttemptsPrefix, rateKey, cancellationToken).ConfigureAwait(false);
            attempts++;
            if (attempts >= MaxAttempts)
            {
                await _cache.SetAsync(LockoutPrefix, rateKey, "locked", AttemptTtl, cancellationToken).ConfigureAwait(false);
                await _cache.RemoveAsync(AttemptsPrefix, rateKey, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _cache.SetAsync(AttemptsPrefix, rateKey, attempts, AttemptTtl, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
