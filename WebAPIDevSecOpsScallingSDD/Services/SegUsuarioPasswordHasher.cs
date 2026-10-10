using System;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface ISegUsuarioPasswordHasher
    {
        string Hash(string plano);

        bool Verify(string plano, string hash);

        bool NeedsRehash(string hash);
    }

    // Fake temporal determinista para no bloquear slices. No usar como implementacion final en prod.
    // Se mantiene para los ~30 tests existentes; 04-02 añade Argon2id como implementacion real (DI swap).
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

        public bool NeedsRehash(string hash)
        {
            ArgumentNullException.ThrowIfNull(hash);
            return false;
        }
    }

    /// <summary>Hasher real Argon2id (64MB/3 iter) con fallback BCrypt solo migracion (04-02).</summary>
    /// <remarks>
    /// Formato propio PHC: $argon2id$v=19$m={mem},t={iter},p={lanes}${saltB64}${hashB64}.
    /// BCrypt ($2a$/$2b$) solo se verifica, nunca se genera; cada hash BCrypt necesita rehash.
    /// Sin persistencia de rehash en esta fase (LoginService lee con AsNoTracking): ver NOTE (04-02) y spec.md Limites.
    /// Nunca loguear plano/salt/hash aqui.
    /// </remarks>
    public sealed class Argon2IdSegUsuarioPasswordHasher : ISegUsuarioPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Version = 19;

        private readonly PasswordHasherOptions _options;
        private readonly int _degreeOfParallelism;

        public Argon2IdSegUsuarioPasswordHasher()
            : this(null)
        {
        }

        public Argon2IdSegUsuarioPasswordHasher(PasswordHasherOptions? options)
        {
            _options = options ?? new PasswordHasherOptions();
            if (_options.MemoryKBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "MemoryKBytes must be positive.");
            }

            if (_options.Iterations <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "Iterations must be positive.");
            }

            // Constante interna (no umbral de spec): determinista y acotada para CI/NBomber.
            _degreeOfParallelism = Math.Max(1, Math.Min(4, Environment.ProcessorCount));
        }

        public string Hash(string plano)
        {
            ArgumentNullException.ThrowIfNull(plano);
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var derived = ComputeHash(plano, salt, _options.MemoryKBytes, _options.Iterations, _degreeOfParallelism);
            return $"$argon2id$v={Version}$m={_options.MemoryKBytes},t={_options.Iterations},p={_degreeOfParallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(derived)}";
        }

        public bool Verify(string plano, string hash)
        {
            ArgumentNullException.ThrowIfNull(plano);
            ArgumentNullException.ThrowIfNull(hash);
            if (IsBCryptHash(hash))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(plano, hash);
                }
                catch (Exception ex) when (ex is BCrypt.Net.SaltParseException || ex is ArgumentException)
                {
                    // Hash BCrypt corrupto: fail-closed a 401 genérico, nunca 500.
                    return false;
                }
            }

            if (!TryParseArgon2id(hash, out var memory, out var iterations, out var parallelism, out var salt, out var expected))
            {
                return false;
            }

            try
            {
                var actual = ComputeHash(plano, salt, memory, iterations, parallelism);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                // Hash corrupto con parámetros fuera del rango del proveedor: fail-closed a 401 genérico, nunca 500.
                return false;
            }
        }

        public bool NeedsRehash(string hash)
        {
            ArgumentNullException.ThrowIfNull(hash);
            if (IsBCryptHash(hash))
            {
                return true;
            }

            if (TryParseArgon2id(hash, out var memory, out var iterations, out _, out _, out _))
            {
                return memory != _options.MemoryKBytes || iterations != _options.Iterations;
            }

            return true;
        }

        private static bool IsBCryptHash(string hash)
        {
            return hash.StartsWith("$2a$", StringComparison.Ordinal) || hash.StartsWith("$2b$", StringComparison.Ordinal);
        }

        private static byte[] ComputeHash(string plano, byte[] salt, int memoryKBytes, int iterations, int parallelism)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(plano);
            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = salt,
                MemorySize = memoryKBytes,
                Iterations = iterations,
                DegreeOfParallelism = parallelism,
            };
            return argon2.GetBytes(HashSize);
        }

        private static bool TryParseArgon2id(string hash, out int memory, out int iterations, out int parallelism, out byte[] salt, out byte[] expected)
        {
            memory = 0;
            iterations = 0;
            parallelism = 0;
            salt = [];
            expected = [];
            var parts = hash.Split('$');
            if (parts.Length != 6 || !string.Equals(parts[1], "argon2id", StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(parts[2], $"v={Version}", StringComparison.Ordinal))
            {
                return false;
            }

            var paramParts = parts[3].Split(',');
            if (paramParts.Length != 3)
            {
                return false;
            }

            if (!paramParts[0].StartsWith("m=", StringComparison.Ordinal)
                || !int.TryParse(paramParts[0].AsSpan(2), out memory)
                || !paramParts[1].StartsWith("t=", StringComparison.Ordinal)
                || !int.TryParse(paramParts[1].AsSpan(2), out iterations)
                || !paramParts[2].StartsWith("p=", StringComparison.Ordinal)
                || !int.TryParse(paramParts[2].AsSpan(2), out parallelism))
            {
                return false;
            }

            if (memory <= 0 || iterations <= 0 || parallelism <= 0)
            {
                return false;
            }

            try
            {
                salt = Convert.FromBase64String(parts[4]);
                expected = Convert.FromBase64String(parts[5]);
            }
            catch (FormatException)
            {
                return false;
            }

            return salt.Length == SaltSize && expected.Length == HashSize;
        }
    }
}
