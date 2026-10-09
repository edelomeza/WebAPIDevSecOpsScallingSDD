using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;
using UsuarioModel = WebAPIDevSecOpsScallingSDD.Models.SegUsuario;

namespace UnitTest.Login2Fa
{
    public class Login2FaServiceTests
    {
        private const string Password = "Secreto123";
        private const string TotpSecret = "JBSWY3DPEHPK3PXP";
        private const string ValidCode = "123456";

        private static readonly IDataProtectionProvider TestProvider = DataProtectionProvider.Create("Test-Login2Fa-Shared");

        private static TwoFactorSecretProtector CreateProtector() => new TwoFactorSecretProtector(TestProvider);

        private static string ProtectSecret() => CreateProtector().Protect(TotpSecret);

        [Fact]
        public async Task NullRequestThrowsArgumentNull()
        {
            await using var context = CreateContext();
            var service = new Login2FaService(context, new FakeCacheService(), new FakeTotpService(), CreateProtector());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.VerifyAsync(null!));
        }

        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            var cache = new FakeCacheService();
            var totp = new FakeTotpService();
            var protector = CreateProtector();
            using var context = CreateContext();

            Assert.Throws<ArgumentNullException>(() => new Login2FaService(null!, cache, totp, protector));
            Assert.Throws<ArgumentNullException>(() => new Login2FaService(context, null!, totp, protector));
            Assert.Throws<ArgumentNullException>(() => new Login2FaService(context, cache, null!, protector));
            Assert.Throws<ArgumentNullException>(() => new Login2FaService(context, cache, totp, null!));
        }

        [Fact]
        public async Task NullBlankAndNonHexCredentialsReturnInvalidWithoutQueryingCache()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new Login2FaService(context, cache, new FakeTotpService(), CreateProtector());

            var nullTemp = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = null!, TotpCode = ValidCode });
            var nullCode = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = "ABCDEF", TotpCode = null! });
            var blank = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = "   ", TotpCode = "   " });
            var blankCode = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = new string('C', 64), TotpCode = "   " });
            var blankTemp = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = "   ", TotpCode = ValidCode });
            var forbidden = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = "token123", TotpCode = ValidCode });

            Assert.Equal(Login2FaStatus.InvalidCredentials, nullTemp.Status);
            Assert.Equal(Login2FaStatus.InvalidCredentials, nullCode.Status);
            Assert.Equal(Login2FaStatus.InvalidCredentials, blank.Status);
            Assert.Equal(Login2FaStatus.InvalidCredentials, blankCode.Status);
            Assert.Equal(Login2FaStatus.InvalidCredentials, blankTemp.Status);
            Assert.Equal(Login2FaStatus.InvalidCredentials, forbidden.Status);
            Assert.Empty(cache.Keys);
            Assert.Empty(cache.Reads);
        }

        [Fact]
        public async Task UnknownTempReturnsInvalidAndSeedsDummyTotp()
        {
            var totp = new RecordingTotp();
            await using var context = CreateContext();
            var service = new Login2FaService(context, new FakeCacheService(), totp, CreateProtector());

            var result = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = new string('A', 64), TotpCode = ValidCode });

            Assert.Equal(Login2FaStatus.InvalidCredentials, result.Status);
            Assert.Null(result.Token);
            Assert.Single(totp.VerifiedSecrets);
            Assert.Equal("login2fa-dummy", totp.VerifiedSecrets[0]);
        }

        [Fact]
        public async Task SuccessVerifyReturnsTokenSingleUseAndClearsAttempts()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Ana2fa",
                strCorreoElectronico = "ana2fa@test.local",
                strPWD = hasher.Hash(Password),
                bln2FAHabilitado = true,
                str2FASecreto = ProtectSecret(),
            });
            await context.SaveChangesAsync();
            var login = new LoginService(context, cache, hasher);
            var service = new Login2FaService(context, cache, new FakeTotpService(), CreateProtector());

            await login.AuthenticateAsync(new LoginRequest { strNombre = "Ana2fa", strPasswordPlano = "Mala1" });
            var issued = await login.AuthenticateAsync(new LoginRequest { strNombre = "Ana2fa", strPasswordPlano = Password });
            var tampered = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = issued.TempCode!, TotpCode = "000000" });
            Assert.Equal(Login2FaStatus.InvalidCredentials, tampered.Status);
            var result = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = issued.TempCode!, TotpCode = ValidCode });

            Assert.Equal(Login2FaStatus.Authenticated, result.Status);
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.Equal(0, await cache.GetAsync<int>("attempts:", "Ana2fa"));
            Assert.DoesNotContain(cache.Keys, key => key.StartsWith("cache:login2fa:", StringComparison.Ordinal));

            var replay = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = issued.TempCode!, TotpCode = ValidCode });

            Assert.Equal(Login2FaStatus.InvalidCredentials, replay.Status);
            Assert.Null(replay.Token);
        }

        [Fact]
        public async Task WrongCodeAndUnknownTempReturnIdenticalInvalid()
        {
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Beto2fa",
                strCorreoElectronico = "beto2fa@test.local",
                strPWD = hasher.Hash(Password),
                bln2FAHabilitado = true,
                str2FASecreto = ProtectSecret(),
            });
            await context.SaveChangesAsync();
            var cache = new FakeCacheService();
            var login = new LoginService(context, cache, hasher);
            var service = new Login2FaService(context, cache, new FakeTotpService(), CreateProtector());

            var issued = await login.AuthenticateAsync(new LoginRequest { strNombre = "Beto2fa", strPasswordPlano = Password });
            var wrong = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = issued.TempCode!, TotpCode = "000000" });
            var unknown = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = new string('B', 64), TotpCode = "000000" });

            Assert.Equal(Login2FaStatus.InvalidCredentials, wrong.Status);
            Assert.Equal(Login2FaStatus.InvalidCredentials, unknown.Status);
            Assert.Null(wrong.Token);
            Assert.Null(unknown.Token);
        }

        [Fact]
        public async Task FiveFailuresThenLockedOut()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Cid2fa",
                strCorreoElectronico = "cid2fa@test.local",
                strPWD = hasher.Hash(Password),
                bln2FAHabilitado = true,
                str2FASecreto = ProtectSecret(),
            });
            await context.SaveChangesAsync();
            var login = new LoginService(context, cache, hasher);
            var service = new Login2FaService(context, cache, new FakeTotpService(), CreateProtector());

            var issued = await login.AuthenticateAsync(new LoginRequest { strNombre = "Cid2fa", strPasswordPlano = Password });
            for (var i = 0; i < 5; i++)
            {
                var failure = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = issued.TempCode!, TotpCode = "000000" });
                Assert.Equal(Login2FaStatus.InvalidCredentials, failure.Status);
            }

            var locked = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = issued.TempCode!, TotpCode = ValidCode });

            Assert.Equal(Login2FaStatus.LockedOut, locked.Status);
            Assert.Null(locked.Token);
            Assert.Contains(cache.Keys, key => key == "lockout:Cid2fa");
            Assert.Equal(0, await cache.GetAsync<int>("attempts:", "Cid2fa"));
        }

        [Fact]
        public async Task DeletedUserAfterIssueReturnsInvalid()
        {
            var cache = new FakeCacheService();
            var totp = new RecordingTotp();
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            var user = new UsuarioModel
            {
                strNombre = "Eva2fa",
                strCorreoElectronico = "eva2fa@test.local",
                strPWD = hasher.Hash(Password),
                bln2FAHabilitado = true,
                str2FASecreto = ProtectSecret(),
            };
            context.SegUsuarios.Add(user);
            await context.SaveChangesAsync();
            var login = new LoginService(context, cache, hasher);
            var service = new Login2FaService(context, cache, totp, CreateProtector());

            var issued = await login.AuthenticateAsync(new LoginRequest { strNombre = "Eva2fa", strPasswordPlano = Password });
            context.SegUsuarios.Remove(user);
            await context.SaveChangesAsync();

            var result = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = issued.TempCode!, TotpCode = ValidCode });

            Assert.Equal(Login2FaStatus.InvalidCredentials, result.Status);
            Assert.Null(result.Token);
            Assert.Single(totp.VerifiedSecrets);
            Assert.Equal("login2fa-dummy", totp.VerifiedSecrets[0]);
        }

        [Fact]
        public async Task Disabled2faAfterIssueReturnsInvalid()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            var user = new UsuarioModel
            {
                strNombre = "Gil2fa",
                strCorreoElectronico = "gil2fa@test.local",
                strPWD = hasher.Hash(Password),
                bln2FAHabilitado = true,
                str2FASecreto = ProtectSecret(),
            };
            context.SegUsuarios.Add(user);
            await context.SaveChangesAsync();
            var login = new LoginService(context, cache, hasher);
            var service = new Login2FaService(context, cache, new FakeTotpService(), CreateProtector());

            var issued = await login.AuthenticateAsync(new LoginRequest { strNombre = "Gil2fa", strPasswordPlano = Password });
            user.bln2FAHabilitado = false;
            await context.SaveChangesAsync();

            var result = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = issued.TempCode!, TotpCode = ValidCode });

            Assert.Equal(Login2FaStatus.InvalidCredentials, result.Status);
            Assert.Null(result.Token);
            Assert.Equal(1, await cache.GetAsync<int>("attempts:", "Gil2fa"));
        }

        [Fact]
        public async Task MissingTotpSecretAfterIssueReturnsInvalid()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            var user = new UsuarioModel
            {
                strNombre = "Ian2fa",
                strCorreoElectronico = "ian2fa@test.local",
                strPWD = hasher.Hash(Password),
                bln2FAHabilitado = true,
                str2FASecreto = ProtectSecret(),
            };
            context.SegUsuarios.Add(user);
            await context.SaveChangesAsync();
            var login = new LoginService(context, cache, hasher);
            var service = new Login2FaService(context, cache, new FakeTotpService(), CreateProtector());

            var issued = await login.AuthenticateAsync(new LoginRequest { strNombre = "Ian2fa", strPasswordPlano = Password });
            user.str2FASecreto = null;
            await context.SaveChangesAsync();

            var result = await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = issued.TempCode!, TotpCode = ValidCode });

            Assert.Equal(Login2FaStatus.InvalidCredentials, result.Status);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task LockoutUsesExpectedKeysAndTtl()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Key2fa",
                strCorreoElectronico = "key2fa@test.local",
                strPWD = hasher.Hash(Password),
                bln2FAHabilitado = true,
                str2FASecreto = ProtectSecret(),
            });
            await context.SaveChangesAsync();
            var login = new LoginService(context, cache, hasher);
            var service = new Login2FaService(context, cache, new FakeTotpService(), CreateProtector());

            var issued = await login.AuthenticateAsync(new LoginRequest { strNombre = "Key2fa", strPasswordPlano = Password });
            await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = issued.TempCode!, TotpCode = "000000" });

            Assert.Contains(cache.Keys, key => key == "attempts:Key2fa");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(120), ttl));
        }

        [Fact]
        public async Task ReadsDoNotTrackEntities()
        {
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Track2fa",
                strCorreoElectronico = "track2fa@test.local",
                strPWD = hasher.Hash(Password),
                bln2FAHabilitado = true,
                str2FASecreto = ProtectSecret(),
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var cache = new FakeCacheService();
            var login = new LoginService(context, cache, hasher);
            var service = new Login2FaService(context, cache, new FakeTotpService(), CreateProtector());

            var issued = await login.AuthenticateAsync(new LoginRequest { strNombre = "Track2fa", strPasswordPlano = Password });
            await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = issued.TempCode!, TotpCode = ValidCode });
            await service.VerifyAsync(new Login2FaVerifyRequest { TempToken = issued.TempCode!, TotpCode = "000000" });

            Assert.Empty(context.ChangeTracker.Entries());
        }

        [Fact]
        public void FakeTotpAcceptsOnlyExpectedCodeWithSecret()
        {
            var totp = new FakeTotpService();

            Assert.True(totp.Verify(TotpSecret, ValidCode));
            Assert.False(totp.Verify(TotpSecret, "000000"));
            Assert.False(totp.Verify(string.Empty, ValidCode));
        }

        [Fact]
        public void FakeTotpNullSecretThrowsArgumentNull()
        {
            var totp = new FakeTotpService();

            Assert.Throws<ArgumentNullException>(() => totp.Verify(null!, ValidCode));
        }

        [Fact]
        public void FakeTotpNullCodeThrowsArgumentNull()
        {
            var totp = new FakeTotpService();

            Assert.Throws<ArgumentNullException>(() => totp.Verify(TotpSecret, null!));
        }

        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
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

            public readonly List<string> Reads = new();

            public readonly List<TimeSpan> Ttls = new();

            public Task<T?> GetAsync<T>(string prefix, string key, CancellationToken cancellationToken = default)
            {
                Reads.Add(prefix + key);
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
