using System;
using OtpNet;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface ITotpProvisioner
    {
        string GenerateSecret();

        Uri BuildOtpAuthUri(string secret, string account, string issuer);
    }

    public sealed class OtpNetTotpService : ITotpService, ITotpProvisioner
    {
        public string GenerateSecret()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        public Uri BuildOtpAuthUri(string secret, string account, string issuer)
        {
            ArgumentNullException.ThrowIfNull(secret);
            ArgumentNullException.ThrowIfNull(account);
            ArgumentNullException.ThrowIfNull(issuer);
            var cleanSecret = secret.Trim();
            var cleanAccount = account.Trim();
            var cleanIssuer = issuer.Trim();
            if (cleanSecret.Length == 0)
            {
                throw new ArgumentException("Secret is required.", nameof(secret));
            }

            if (cleanAccount.Length == 0)
            {
                throw new ArgumentException("Account is required.", nameof(account));
            }

            if (cleanIssuer.Length == 0)
            {
                throw new ArgumentException("Issuer is required.", nameof(issuer));
            }

            var escapedAccount = Uri.EscapeDataString(cleanAccount);
            var escapedIssuer = Uri.EscapeDataString(cleanIssuer);
            return new Uri($"otpauth://totp/{escapedIssuer}:{escapedAccount}?secret={cleanSecret}&issuer={escapedIssuer}&digits=6&period=30", UriKind.Absolute);
        }

        public bool Verify(string secret, string code)
        {
            ArgumentNullException.ThrowIfNull(secret);
            ArgumentNullException.ThrowIfNull(code);
            var cleanSecret = secret.Trim();
            var cleanCode = code.Trim();
            if (cleanSecret.Length == 0 || cleanCode.Length == 0)
            {
                return false;
            }

            try
            {
                var bytes = Base32Encoding.ToBytes(cleanSecret);
                var totp = new Totp(bytes);
                return totp.VerifyTotp(cleanCode, out _, new VerificationWindow(previous: 1, future: 1));
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
