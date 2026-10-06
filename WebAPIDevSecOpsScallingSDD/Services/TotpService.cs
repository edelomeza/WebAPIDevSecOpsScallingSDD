using System;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface ITotpService
    {
        bool Verify(string secret, string code);
    }

    // NOTE (03-09): stub temporal determinista para no bloquear el slice 03-07.
    // Reemplazar con OtpNet real con ventana ±1 paso. No usar como implementacion final.
    public sealed class FakeTotpService : ITotpService
    {
        private const string ExpectedCode = "123456";

        public bool Verify(string secret, string code)
        {
            ArgumentNullException.ThrowIfNull(secret);
            ArgumentNullException.ThrowIfNull(code);
            return secret.Length != 0 && string.Equals(code, ExpectedCode, StringComparison.Ordinal);
        }
    }
}
