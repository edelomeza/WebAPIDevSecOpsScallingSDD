using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;
using UsuarioModel = WebAPIDevSecOpsScallingSDD.Models.SegUsuario;

namespace UnitTest.TwoFactor
{
    public class TwoFactorServiceTests
    {
        private const string Password = "Secreto123";

        private static readonly IDataProtectionProvider TestProvider = DataProtectionProvider.Create("Test-TwoFactor-Shared");

        private static TwoFactorSecretProtector CreateProtector() => new TwoFactorSecretProtector(TestProvider);

        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            var cache = new FakeCacheService();
            var totp = new FakeTotpService();
            var provisioner = new StubProvisioner();
            var protector = CreateProtector();
            using var context = CreateContext();

            Assert.Throws<ArgumentNullException>(() => new TwoFactorService(null!, cache, totp, provisioner, protector));
            Assert.Throws<ArgumentNullException>(() => new TwoFactorService(context, null!, totp, provisioner, protector));
            Assert.Throws<ArgumentNullException>(() => new TwoFactorService(context, cache, null!, provisioner, protector));
            Assert.Throws<ArgumentNullException>(() => new TwoFactorService(context, cache, totp, null!, protector));
            Assert.Throws<ArgumentNullException>(() => new TwoFactorService(context, cache, totp, provisioner, null!));
        }

        [Fact]
        public async Task NullUserIdAndRequestThrowArgumentNull()
        {
            await using var context = CreateContext();
            var service = CreateService(context);

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.SetupAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.VerifyAsync("1", null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.VerifyAsync(null!, new TwoFactorVerifyRequest { TotpCode = "123456" }));
        }

        [Fact]
        public async Task SetupStoresProtectedSecretAndDisables2Fa()
        {
            var protector = CreateProtector();
            await using var context = CreateContext();
            var userId = await SeedUserAsync(context, "SetupAna");
            var provisioner = new StubProvisioner("JBSWY3DPEHPK3PXP");
            var service = new TwoFactorService(context, new FakeCacheService(), new FakeTotpService(), provisioner, protector);

            var response = await service.SetupAsync(userId);

            Assert.NotNull(response);
            Assert.False(string.IsNullOrEmpty(response.Secret));
            Assert.StartsWith("otpauth://", response.OtpAuthUri, StringComparison.Ordinal);
            Assert.Contains(response.Secret, response.OtpAuthUri, StringComparison.Ordinal);
            var stored = await context.SegUsuarios.FirstAsync(e => e.id == int.Parse(userId, CultureInfo.InvariantCulture));
            Assert.NotEqual(response.Secret, stored.str2FASecreto);
            Assert.Equal(response.Secret, protector.Unprotect(stored.str2FASecreto!));
            Assert.False(stored.bln2FAHabilitado);
        }

        [Fact]
        public async Task SetupWithUnknownUserReturnsNull()
        {
            await using var context = CreateContext();
            var service = CreateService(context);

            Assert.Null(await service.SetupAsync("no-numerico"));
            Assert.Null(await service.SetupAsync("999999"));
        }

        [Fact]
        public async Task SetupEmptyUserIdThrowsArgumentException()
        {
            await using var context = CreateContext();
            var service = CreateService(context);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.SetupAsync("   "));

            Assert.Equal("userId", ex.ParamName);
            Assert.Contains("User id is required.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task VerifyEmptyUserIdThrowsArgumentException()
        {
            await using var context = CreateContext();
            var service = CreateService(context);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.VerifyAsync("   ", new TwoFactorVerifyRequest { TotpCode = "123456" }));

            Assert.Equal("userId", ex.ParamName);
            Assert.Contains("User id is required.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task VerifyNullAndEmptyCodeReturnInvalid()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var userId = await SeedUserAsync(context, "VerifyNulo");
            var service = CreateService(context, cache);
            await service.SetupAsync(userId);

            var nullCode = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = null! });
            var emptyCode = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "   " });

            Assert.Equal(TwoFactorStatus.InvalidCode, nullCode.Status);
            Assert.Equal(TwoFactorStatus.InvalidCode, emptyCode.Status);
            Assert.Empty(cache.Keys);
        }

        [Fact]
        public async Task VerifyNonNumericUserIdReturnsInvalidAndRegistersFailure()
        {
            var totp = new RecordingTotp();
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new TwoFactorService(context, cache, totp, new StubProvisioner(), CreateProtector());

            var result = await service.VerifyAsync("no-numerico", new TwoFactorVerifyRequest { TotpCode = "123456" });

            Assert.Equal(TwoFactorStatus.InvalidCode, result.Status);
            Assert.Contains("twofactor-dummy", totp.VerifiedSecrets);
            Assert.Equal(1, await cache.GetAsync<int>("attempts:", "2fa:no-numerico"));
        }

        [Fact]
        public async Task VerifyWhitespaceStoredSecretReturnsInvalid()
        {
            var totp = new RecordingTotp();
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var userId = await SeedUserAsync(context, "VerifyBlanco", "   ");
            var service = new TwoFactorService(context, cache, totp, new StubProvisioner(), CreateProtector());

            var result = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "123456" });

            Assert.Equal(TwoFactorStatus.InvalidCode, result.Status);
            Assert.Contains("twofactor-dummy", totp.VerifiedSecrets);
            Assert.Equal(1, await cache.GetAsync<int>("attempts:", $"2fa:{userId}"));
        }

        [Fact]
        public async Task VerifySuccessEnables2FaAndClearsAttempts()
        {
            var cache = new FakeCacheService();
            var protector = CreateProtector();
            await using var context = CreateContext();
            var userId = await SeedUserAsync(context, "VerifyBeto");
            var service = new TwoFactorService(context, cache, new FakeTotpService(), new StubProvisioner(), protector);
            var setup = await service.SetupAsync(userId);

            Assert.NotNull(setup);
            var tampered = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "000000" });
            Assert.Equal(TwoFactorStatus.InvalidCode, tampered.Status);
            var result = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "123456" });

            Assert.Equal(TwoFactorStatus.Enabled, result.Status);
            Assert.True((await context.SegUsuarios.FirstAsync(e => e.id == int.Parse(userId, CultureInfo.InvariantCulture))).bln2FAHabilitado);
            Assert.Equal(0, await cache.GetAsync<int>("attempts:", $"2fa:{userId}"));
            Assert.DoesNotContain(setup.Secret, cache.KeysToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task WrongCodeAndUnknownUserReturnIdenticalInvalid()
        {
            var totp = new RecordingTotp();
            await using var context = CreateContext();
            var protector = CreateProtector();
            var cache = new FakeCacheService();
            var userId = await SeedUserAsync(context, "VerifyCid");
            var service = new TwoFactorService(context, cache, totp, new StubProvisioner("JBSWY3DPEHPK3PXP"), protector);
            await service.SetupAsync(userId);

            var wrong = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "000000" });
            var unknown = await service.VerifyAsync("999999", new TwoFactorVerifyRequest { TotpCode = "000000" });

            Assert.Equal(TwoFactorStatus.InvalidCode, wrong.Status);
            Assert.Equal(TwoFactorStatus.InvalidCode, unknown.Status);
            Assert.Contains("twofactor-dummy", totp.VerifiedSecrets);
            Assert.Equal(1, await cache.GetAsync<int>("attempts:", $"2fa:{userId}"));
            Assert.Equal(1, await cache.GetAsync<int>("attempts:", "2fa:999999"));
        }

        [Fact]
        public async Task FiveFailuresThenLockedOut()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var userId = await SeedUserAsync(context, "VerifyDan");
            var service = CreateService(context, cache);
            await service.SetupAsync(userId);

            for (var i = 0; i < 5; i++)
            {
                var failure = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "000000" });
                Assert.Equal(TwoFactorStatus.InvalidCode, failure.Status);
            }

            var locked = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "123456" });

            Assert.Equal(TwoFactorStatus.LockedOut, locked.Status);
            Assert.Contains(cache.Keys, key => key == $"lockout:2fa:{userId}");
            Assert.Equal(0, await cache.GetAsync<int>("attempts:", $"2fa:{userId}"));
        }

        [Fact]
        public async Task TamperedProtectedPayloadReturnsInvalid()
        {
            var totp = new RecordingTotp();
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var userId = await SeedUserAsync(context, "VerifyEva", "no-es-un-payload-protegido");
            var service = new TwoFactorService(context, cache, totp, new StubProvisioner(), CreateProtector());

            var result = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "123456" });

            Assert.Equal(TwoFactorStatus.InvalidCode, result.Status);
            Assert.Contains("twofactor-dummy", totp.VerifiedSecrets);
            Assert.Equal(1, await cache.GetAsync<int>("attempts:", $"2fa:{userId}"));
        }

        [Fact]
        public async Task ResestupInvalidatesPreviousSecret()
        {
            var cache = new FakeCacheService();
            var protector = CreateProtector();
            await using var context = CreateContext();
            var userId = await SeedUserAsync(context, "VerifyGil");
            var provisioner = new StubProvisioner("AAAAAAAABBBBBBBBCCCCCCCC", "DDDDDDDDEEEEEEEEFFFFFFFF");
            var totp = new MappingTotp(new Dictionary<string, string>
            {
                ["AAAAAAAABBBBBBBBCCCCCCCC"] = "111111",
                ["DDDDDDDDEEEEEEEEFFFFFFFF"] = "222222",
            });
            var service = new TwoFactorService(context, cache, totp, provisioner, protector);

            await service.SetupAsync(userId);
            var firstOldCode = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "111111" });
            Assert.Equal(TwoFactorStatus.Enabled, firstOldCode.Status);

            await service.SetupAsync(userId);
            var oldAfterResestup = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "111111" });
            var current = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "222222" });

            Assert.Equal(TwoFactorStatus.InvalidCode, oldAfterResestup.Status);
            Assert.Equal(TwoFactorStatus.Enabled, current.Status);
        }

        [Fact]
        public async Task SetupDoesNotResetAttempts()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var userId = await SeedUserAsync(context, "VerifyIan");
            var service = CreateService(context, cache);
            await service.SetupAsync(userId);

            await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "000000" });
            await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "000000" });
            await service.SetupAsync(userId);

            Assert.Equal(2, await cache.GetAsync<int>("attempts:", $"2fa:{userId}"));
        }

        [Fact]
        public async Task VerifyWith2FaAlreadyEnabledStillSucceeds()
        {
            await using var context = CreateContext();
            var userId = await SeedUserAsync(context, "VerifyJon");
            var service = CreateService(context);
            await service.SetupAsync(userId);

            var first = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "123456" });
            var second = await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "123456" });

            Assert.Equal(TwoFactorStatus.Enabled, first.Status);
            Assert.Equal(TwoFactorStatus.Enabled, second.Status);
        }

        [Fact]
        public async Task LockoutUsesExpectedKeysAndTtl()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var userId = await SeedUserAsync(context, "VerifyKey");
            var service = CreateService(context, cache);
            await service.SetupAsync(userId);

            await service.VerifyAsync(userId, new TwoFactorVerifyRequest { TotpCode = "000000" });

            Assert.Contains(cache.Keys, key => key == $"attempts:2fa:{userId}");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(120), ttl));
        }

        private static TwoFactorService CreateService(AppDbContext context, FakeCacheService? cache = null)
        {
            return new TwoFactorService(
                context,
                cache ?? new FakeCacheService(),
                new FakeTotpService(),
                new StubProvisioner("JBSWY3DPEHPK3PXP"),
                CreateProtector());
        }

        private static async Task<string> SeedUserAsync(AppDbContext context, string nombre, string? rawProtected = null)
        {
            var hasher = new FakeSegUsuarioPasswordHasher();
            var protector = CreateProtector();
            var entity = new UsuarioModel
            {
                strNombre = nombre,
                strCorreoElectronico = $"{nombre}@test.local",
                strPWD = hasher.Hash(Password),
                str2FASecreto = rawProtected ?? protector.Protect("JBSWY3DPEHPK3PXP"),
            };
            context.SegUsuarios.Add(entity);
            await context.SaveChangesAsync().ConfigureAwait(false);
            return entity.id.ToString(CultureInfo.InvariantCulture);
        }

        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private sealed class StubProvisioner : ITotpProvisioner
        {
            private static readonly string[] DefaultSecrets = new[] { "JBSWY3DPEHPK3PXP" };

            private readonly Queue<string> _secrets;

            public StubProvisioner(params string[] secrets)
            {
                _secrets = new Queue<string>(secrets.Length == 0 ? DefaultSecrets : secrets);
            }

            public string GenerateSecret()
            {
                return _secrets.Count == 1 ? _secrets.Peek() : _secrets.Dequeue();
            }

            public Uri BuildOtpAuthUri(string secret, string account, string issuer)
            {
                return new Uri($"otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}&digits=6&period=30", UriKind.Absolute);
            }
        }

        private sealed class MappingTotp : ITotpService
        {
            private readonly Dictionary<string, string> _codes;

            public MappingTotp(Dictionary<string, string> codes)
            {
                _codes = codes;
            }

            public bool Verify(string secret, string code)
            {
                return _codes.TryGetValue(secret, out var expected) && string.Equals(expected, code, StringComparison.Ordinal);
            }
        }

        private sealed class RecordingTotp : ITotpService
        {
            private readonly FakeTotpService _inner = new();

            public readonly List<string> VerifiedSecrets = new();

            public bool Verify(string secret, string code)
            {
                VerifiedSecrets.Add(secret);
                return _inner.Verify(secret, code);
            }
        }

        private sealed class FakeCacheService : ICacheService
        {
            private readonly Dictionary<string, object?> _store = new();

            public List<string> Keys => new(_store.Keys);

            public readonly List<TimeSpan> Ttls = new();

            public string KeysToString() => string.Join(";", _store.Keys);

            public Task<T?> GetAsync<T>(string prefix, string key, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_store.TryGetValue(prefix + key, out var value) ? (T?)value : default);
            }

            public Task SetAsync<T>(string prefix, string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
            {
                _store[prefix + key] = value;
                Ttls.Add(ttl);
                return Task.CompletedTask;
            }

            public Task RemoveAsync(string prefix, string key, CancellationToken cancellationToken = default)
            {
                _store.Remove(prefix + key);
                return Task.CompletedTask;
            }
        }
    }
}
