using System;
using WebAPIDevSecOpsScallingSDD.Services;

namespace UnitTest.Login
{
    public class PasswordHasherTests
    {
        [Fact]
        public void Argon2idHashVerifiesCorrectPassword()
        {
            var hasher = new Argon2IdSegUsuarioPasswordHasher();

            var hash = hasher.Hash("Secreto123");

            Assert.StartsWith("$argon2id$v=19$m=65536,t=3,", hash, StringComparison.Ordinal);
            Assert.True(hasher.Verify("Secreto123", hash));
        }

        [Fact]
        public void Argon2idRejectsWrongPassword()
        {
            var hasher = new Argon2IdSegUsuarioPasswordHasher();

            var hash = hasher.Hash("Secreto123");

            Assert.False(hasher.Verify("OtraClave456", hash));
        }

        [Fact]
        public void BCryptLegacyVerifiesAndNeedsRehash()
        {
            var hasher = new Argon2IdSegUsuarioPasswordHasher();
            var legacy = BCrypt.Net.BCrypt.HashPassword("Secreto123", 4);

            Assert.True(hasher.Verify("Secreto123", legacy));
            Assert.False(hasher.Verify("OtraClave456", legacy));
            Assert.True(hasher.NeedsRehash(legacy));
        }

        [Fact]
        public void FreshArgon2idDoesNotNeedRehashButUnknownDoes()
        {
            var hasher = new Argon2IdSegUsuarioPasswordHasher();

            var fresh = hasher.Hash("Secreto123");

            Assert.False(hasher.NeedsRehash(fresh));
            Assert.True(hasher.NeedsRehash("no-es-un-hash"));
            Assert.False(hasher.Verify("Secreto123", "no-es-un-hash"));
        }

        [Fact]
        public void NullInputsThrowArgumentNull()
        {
            var hasher = new Argon2IdSegUsuarioPasswordHasher();
            var fake = new FakeSegUsuarioPasswordHasher();

            Assert.Throws<ArgumentNullException>(() => hasher.Hash(null!));
            Assert.Throws<ArgumentNullException>(() => hasher.Verify(null!, "h"));
            Assert.Throws<ArgumentNullException>(() => hasher.Verify("p", null!));
            Assert.Throws<ArgumentNullException>(() => hasher.NeedsRehash(null!));
            Assert.False(fake.NeedsRehash("cualquier-hash"));
        }

        [Fact]
        public void FakeNullInputsThrowArgumentNull()
        {
            var fake = new FakeSegUsuarioPasswordHasher();

            Assert.Throws<ArgumentNullException>(() => fake.Hash(null!));
            Assert.Throws<ArgumentNullException>(() => fake.Verify(null!, "h"));
            Assert.Throws<ArgumentNullException>(() => fake.Verify("p", null!));
            Assert.Throws<ArgumentNullException>(() => fake.NeedsRehash(null!));
        }

        [Fact]
        public void ZeroCostOptionsThrowArgumentOutOfRange()
        {
            var memoryEx = Assert.Throws<ArgumentOutOfRangeException>(() => new Argon2IdSegUsuarioPasswordHasher(new PasswordHasherOptions { MemoryKBytes = 0, Iterations = 3 }));
            var iterationsEx = Assert.Throws<ArgumentOutOfRangeException>(() => new Argon2IdSegUsuarioPasswordHasher(new PasswordHasherOptions { MemoryKBytes = 65536, Iterations = 0 }));

            Assert.Contains("MemoryKBytes must be positive.", memoryEx.Message, StringComparison.Ordinal);
            Assert.Contains("Iterations must be positive.", iterationsEx.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void CustomOptionsAreHonoredAndDetectedAsRehash()
        {
            var custom = new Argon2IdSegUsuarioPasswordHasher(new PasswordHasherOptions { MemoryKBytes = 65536, Iterations = 2 });
            var standard = new Argon2IdSegUsuarioPasswordHasher();

            var hash = custom.Hash("Secreto123");

            Assert.Contains("m=65536,t=2,", hash, StringComparison.Ordinal);
            Assert.True(custom.Verify("Secreto123", hash));
            Assert.False(custom.NeedsRehash(hash));
            Assert.True(standard.NeedsRehash(hash));
        }

        [Fact]
        public void ParallelismIsPinnedInHash()
        {
            var hasher = new Argon2IdSegUsuarioPasswordHasher();
            var expected = Math.Max(1, Math.Min(4, Environment.ProcessorCount));

            var hash = hasher.Hash("Secreto123");

            Assert.Contains($"p={expected}$", hash, StringComparison.Ordinal);
        }

        [Fact]
        public void MalformedHashesAreRejectedAndNeedRehash()
        {
            var hasher = new Argon2IdSegUsuarioPasswordHasher();
            var fresh = hasher.Hash("Secreto123");
            var parts = fresh.Split('$');
            var lanes = Math.Max(1, Math.Min(4, Environment.ProcessorCount));
            var malformed = new[]
            {
                fresh.Replace("$argon2id$", "$argon2i$", StringComparison.Ordinal),
                fresh.Replace("$v=19$", "$v=18$", StringComparison.Ordinal),
                $"$argon2id$v=19$m=65536,t=3${parts[4]}${parts[5]}",
                $"$argon2id$v=19$x=1,t=3,p={lanes}${parts[4]}${parts[5]}",
                $"$argon2id$v=19$m=abc,t=3,p={lanes}${parts[4]}${parts[5]}",
                $"$argon2id$v=19$m=0,t=3,p={lanes}${parts[4]}${parts[5]}",
                $"$argon2id$v=19$m=1,t=3,p={lanes}${parts[4]}${parts[5]}",
                $"$argon2id$v=19$m=65536,t=3,p=0${parts[4]}${parts[5]}",
                "$2b$12$short",
                $"$argon2id$v=19$m=65536,t=3,p={lanes}$!!!${parts[5]}",
                $"$argon2id$v=19$m=65536,t=3,p={lanes}${Convert.ToBase64String(new byte[8])}${parts[5]}",
                $"$argon2id$v=19$ab65536,t=3,p={lanes}${parts[4]}${parts[5]}",
                $"$argon2id$v=19$m=65536,ab3,p={lanes}${parts[4]}${parts[5]}",
                $"$argon2id$v=19$m=65536,t=3,ab{lanes}${parts[4]}${parts[5]}",
            };

            foreach (var hash in malformed)
            {
                Assert.False(hasher.Verify("Secreto123", hash));
                Assert.True(hasher.NeedsRehash(hash));
            }
        }
    }
}
