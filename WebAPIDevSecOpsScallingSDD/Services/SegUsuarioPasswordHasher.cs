using System;
using System.Security.Cryptography;
using System.Text;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface ISegUsuarioPasswordHasher
    {
        string Hash(string plano);

        bool Verify(string plano, string hash);
    }

    // NOTE (04-02): Reemplazar con Argon2id (64MB/3 iter) + fallback BCrypt solo migracion.
    // Stub temporal determinista para no bloquear el slice 03-05. No usar como implementacion final.
    public sealed class FakeSegUsuarioPasswordHasher : ISegUsuarioPasswordHasher
    {
        private const string Salt = "SegUsuario-T1-Salt";

        public string Hash(string plano)
        {
            ArgumentNullException.ThrowIfNull(plano);
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{Salt}:{plano}"));
            return Convert.ToBase64String(bytes);
        }

        public bool Verify(string plano, string hash)
        {
            ArgumentNullException.ThrowIfNull(plano);
            ArgumentNullException.ThrowIfNull(hash);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Hash(plano)),
                Encoding.UTF8.GetBytes(hash));
        }
    }
}
