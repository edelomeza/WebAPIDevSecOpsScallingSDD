using System;
using Microsoft.AspNetCore.DataProtection;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface ITwoFactorSecretProtector
    {
        string Protect(string secret);

        string Unprotect(string protectedPayload);
    }

    public sealed class TwoFactorSecretProtector : ITwoFactorSecretProtector
    {
        private readonly IDataProtector _protector;

        public TwoFactorSecretProtector(IDataProtectionProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            _protector = provider.CreateProtector("TwoFactor");
        }

        public string Protect(string secret)
        {
            ArgumentNullException.ThrowIfNull(secret);
            var clean = secret.Trim();
            if (clean.Length == 0)
            {
                throw new ArgumentException("Secret is required.", nameof(secret));
            }

            return _protector.Protect(clean);
        }

        public string Unprotect(string protectedPayload)
        {
            ArgumentNullException.ThrowIfNull(protectedPayload);
            var clean = protectedPayload.Trim();
            if (clean.Length == 0)
            {
                throw new ArgumentException("Protected payload is required.", nameof(protectedPayload));
            }

            return _protector.Unprotect(clean);
        }
    }
}
