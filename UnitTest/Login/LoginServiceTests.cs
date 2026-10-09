using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;
using UsuarioModel = WebAPIDevSecOpsScallingSDD.Models.SegUsuario;

namespace UnitTest.Login
{
    public class LoginServiceTests
    {
        private const string Password = "Secreto123";

        [Fact]
        public async Task NullRequestThrowsArgumentNull()
        {
            await using var context = CreateContext();
            var service = CreateService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher(), TestTimeProvider.Fixed());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.AuthenticateAsync(null!));
        }

        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            using var context = CreateContext();
            var store = new EfLoginLockoutStore(context, TimeProvider.System);

            Assert.Throws<ArgumentNullException>(() => new LoginService(null!, cache, hasher, store));
            Assert.Throws<ArgumentNullException>(() => new LoginService(context, null!, hasher, store));
            Assert.Throws<ArgumentNullException>(() => new LoginService(context, cache, null!, store));
            Assert.Throws<ArgumentNullException>(() => new LoginService(context, cache, hasher, null!));
        }

        [Fact]
        public async Task SuccessReturnsTokenAndClearsAttempts()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Ana",
                strCorreoElectronico = "ana@test.local",
                strPWD = hasher.Hash(Password),
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var service = CreateService(context, cache, hasher, TestTimeProvider.Fixed());

            await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = "Mala1" });
            await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = "Mala2" });
            var result = await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = Password });

            Assert.Equal(LoginStatus.Authenticated, result.Status);
            Assert.False(string.IsNullOrEmpty(result.Token));
            var row = await context.SegBloqueos.AsNoTracking().FirstOrDefaultAsync(e => e.strNombre == "Ana");
            Assert.NotNull(row);
            Assert.Equal(0, row.intIntentosFallidos);
            Assert.Null(row.dteBloqueoHasta);
        }

        [Fact]
        public async Task UnknownUserAndWrongPasswordReturnIdenticalInvalid()
        {
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Ana",
                strCorreoElectronico = "ana@test.local",
                strPWD = hasher.Hash(Password),
            });
            await context.SaveChangesAsync();
            var service = CreateService(context, new FakeCacheService(), hasher, TestTimeProvider.Fixed());

            var unknown = await service.AuthenticateAsync(new LoginRequest { strNombre = "Nadie", strPasswordPlano = Password });
            var wrong = await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = "Mala1" });

            Assert.Equal(LoginStatus.InvalidCredentials, unknown.Status);
            Assert.Equal(LoginStatus.InvalidCredentials, wrong.Status);
            Assert.Null(unknown.Token);
            Assert.Null(wrong.Token);
        }

        [Fact]
        public async Task BlankCredentialsReturnInvalidWithoutSideEffects()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = CreateService(context, cache, new FakeSegUsuarioPasswordHasher(), TestTimeProvider.Fixed());

            var blankName = await service.AuthenticateAsync(new LoginRequest { strNombre = "   ", strPasswordPlano = Password });
            var blankPassword = await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = string.Empty });

            Assert.Equal(LoginStatus.InvalidCredentials, blankName.Status);
            Assert.Equal(LoginStatus.InvalidCredentials, blankPassword.Status);
            Assert.Empty(cache.Keys);
            Assert.Empty(context.SegBloqueos);
        }

        [Fact]
        public async Task FiveFailuresThenLockedOutPersistently()
        {
            var now = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Ana",
                strCorreoElectronico = "ana@test.local",
                strPWD = hasher.Hash(Password),
            });
            await context.SaveChangesAsync();
            var service = CreateService(context, cache, hasher, new TestTimeProvider(now));

            for (var i = 0; i < 5; i++)
            {
                var failure = await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = "Mala" });
                Assert.Equal(LoginStatus.InvalidCredentials, failure.Status);
            }

            var locked = await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = Password });

            Assert.Equal(LoginStatus.LockedOut, locked.Status);
            Assert.Null(locked.Token);
            var row = await context.SegBloqueos.AsNoTracking().FirstOrDefaultAsync(e => e.strNombre == "Ana");
            Assert.NotNull(row);
            Assert.Equal(now.UtcDateTime + TimeSpan.FromMinutes(15), row.dteBloqueoHasta);
            Assert.DoesNotContain(cache.Keys, key => key.StartsWith("lockout:", StringComparison.Ordinal));
            Assert.DoesNotContain(cache.Keys, key => key.StartsWith("attempts:", StringComparison.Ordinal));
        }

        [Fact]
        public async Task UnknownUserLocksAfterFiveFailures()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = CreateService(context, cache, new FakeSegUsuarioPasswordHasher(), TestTimeProvider.Fixed());

            for (var i = 0; i < 5; i++)
            {
                var failure = await service.AuthenticateAsync(new LoginRequest { strNombre = "Fantasma", strPasswordPlano = "Mala" });
                Assert.Equal(LoginStatus.InvalidCredentials, failure.Status);
            }

            var locked = await service.AuthenticateAsync(new LoginRequest { strNombre = "Fantasma", strPasswordPlano = "Mala" });

            Assert.Equal(LoginStatus.LockedOut, locked.Status);
            Assert.NotNull(await context.SegBloqueos.AsNoTracking().FirstOrDefaultAsync(e => e.strNombre == "Fantasma"));
        }

        [Fact]
        public async Task LockoutExpiresAfter15Minutes()
        {
            var start = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
            var clock = new TestTimeProvider(start);
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Key",
                strCorreoElectronico = "key@test.local",
                strPWD = hasher.Hash(Password),
            });
            await context.SaveChangesAsync();
            var service = CreateService(context, new FakeCacheService(), hasher, clock);

            for (var i = 0; i < 5; i++)
            {
                await service.AuthenticateAsync(new LoginRequest { strNombre = "Key", strPasswordPlano = "Mala" });
            }

            clock.Advance(TimeSpan.FromMinutes(14) + TimeSpan.FromSeconds(59));
            var stillLocked = await service.AuthenticateAsync(new LoginRequest { strNombre = "Key", strPasswordPlano = Password });
            Assert.Equal(LoginStatus.LockedOut, stillLocked.Status);

            clock.Advance(TimeSpan.FromSeconds(2));
            var released = await service.AuthenticateAsync(new LoginRequest { strNombre = "Key", strPasswordPlano = Password });
            Assert.Equal(LoginStatus.Authenticated, released.Status);
        }

        [Fact]
        public async Task SingleFailureAfterExpiryRearmsInsteadOfRelocking()
        {
            var start = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
            var clock = new TestTimeProvider(start);
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Rearme",
                strCorreoElectronico = "rearme@test.local",
                strPWD = hasher.Hash(Password),
            });
            await context.SaveChangesAsync();
            var service = CreateService(context, new FakeCacheService(), hasher, clock);

            for (var i = 0; i < 5; i++)
            {
                await service.AuthenticateAsync(new LoginRequest { strNombre = "Rearme", strPasswordPlano = "Mala" });
            }

            clock.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(1));
            var rearmed = await service.AuthenticateAsync(new LoginRequest { strNombre = "Rearme", strPasswordPlano = "Mala" });
            Assert.Equal(LoginStatus.InvalidCredentials, rearmed.Status);

            var released = await service.AuthenticateAsync(new LoginRequest { strNombre = "Rearme", strPasswordPlano = Password });
            Assert.Equal(LoginStatus.Authenticated, released.Status);
        }

        [Fact]
        public async Task ExpiredLockoutRelocksAfterFiveMoreFailures()
        {
            var start = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
            var clock = new TestTimeProvider(start);
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Rebloqueo",
                strCorreoElectronico = "rebloqueo@test.local",
                strPWD = hasher.Hash(Password),
            });
            await context.SaveChangesAsync();
            var service = CreateService(context, new FakeCacheService(), hasher, clock);

            for (var i = 0; i < 5; i++)
            {
                await service.AuthenticateAsync(new LoginRequest { strNombre = "Rebloqueo", strPasswordPlano = "Mala" });
            }

            clock.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(1));
            for (var i = 0; i < 5; i++)
            {
                var rearmed = await service.AuthenticateAsync(new LoginRequest { strNombre = "Rebloqueo", strPasswordPlano = "Mala" });
                Assert.Equal(LoginStatus.InvalidCredentials, rearmed.Status);
            }

            var relocked = await service.AuthenticateAsync(new LoginRequest { strNombre = "Rebloqueo", strPasswordPlano = Password });
            Assert.Equal(LoginStatus.LockedOut, relocked.Status);
        }

        [Fact]
        public async Task FailureExactlyAtExpiryRearmsInsteadOfRelocking()
        {
            var start = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
            var clock = new TestTimeProvider(start);
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "ExactaFallo",
                strCorreoElectronico = "exactafallo@test.local",
                strPWD = hasher.Hash(Password),
            });
            await context.SaveChangesAsync();
            var service = CreateService(context, new FakeCacheService(), hasher, clock);

            for (var i = 0; i < 5; i++)
            {
                await service.AuthenticateAsync(new LoginRequest { strNombre = "ExactaFallo", strPasswordPlano = "Mala" });
            }

            clock.Advance(TimeSpan.FromMinutes(15));
            var boundary = await service.AuthenticateAsync(new LoginRequest { strNombre = "ExactaFallo", strPasswordPlano = "Mala" });
            Assert.Equal(LoginStatus.InvalidCredentials, boundary.Status);

            var released = await service.AuthenticateAsync(new LoginRequest { strNombre = "ExactaFallo", strPasswordPlano = Password });
            Assert.Equal(LoginStatus.Authenticated, released.Status);
        }

        [Fact]
        public async Task NullCredentialsReturnInvalidWithoutSideEffects()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = CreateService(context, cache, new FakeSegUsuarioPasswordHasher(), TestTimeProvider.Fixed());

            var nullName = await service.AuthenticateAsync(new LoginRequest { strNombre = null!, strPasswordPlano = Password });
            var nullPassword = await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = null! });

            Assert.Equal(LoginStatus.InvalidCredentials, nullName.Status);
            Assert.Equal(LoginStatus.InvalidCredentials, nullPassword.Status);
            Assert.Empty(cache.Keys);
            Assert.Empty(context.SegBloqueos);
        }

        [Fact]
        public void CtorComputesDummyHashFromFixedSeed()
        {
            using var context = CreateContext();
            var hasher = new RecordingHasher();

            _ = CreateService(context, new FakeCacheService(), hasher, TimeProvider.System);

            Assert.Single(hasher.HashedInputs);
            Assert.Equal("login-anti-enumeration-dummy", hasher.HashedInputs[0]);
        }

        [Fact]
        public void LockoutStoreNullDependenciesThrowArgumentNull()
        {
            using var context = CreateContext();
            var clock = TestTimeProvider.Fixed();

            Assert.Throws<ArgumentNullException>(() => new EfLoginLockoutStore(null!, clock));
            Assert.Throws<ArgumentNullException>(() => new EfLoginLockoutStore(context, null!));
        }

        [Fact]
        public async Task LockoutStoreNullNameThrowsArgumentNull()
        {
            await using var context = CreateContext();
            var store = new EfLoginLockoutStore(context, TestTimeProvider.Fixed());

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.IsLockedAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.RecordFailureAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.ResetAsync(null!));
        }

        [Fact]
        public async Task LockReleasesExactlyAtExpiry()
        {
            var start = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
            var clock = new TestTimeProvider(start);
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Exacta",
                strCorreoElectronico = "exacta@test.local",
                strPWD = hasher.Hash(Password),
            });
            await context.SaveChangesAsync();
            var service = CreateService(context, new FakeCacheService(), hasher, clock);

            for (var i = 0; i < 5; i++)
            {
                await service.AuthenticateAsync(new LoginRequest { strNombre = "Exacta", strPasswordPlano = "Mala" });
            }

            clock.Advance(TimeSpan.FromMinutes(15));
            var released = await service.AuthenticateAsync(new LoginRequest { strNombre = "Exacta", strPasswordPlano = Password });

            Assert.Equal(LoginStatus.Authenticated, released.Status);
        }

        [Fact]
        public async Task SuccessfulReadLeavesNoTrackedChanges()
        {
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Track",
                strCorreoElectronico = "track@test.local",
                strPWD = hasher.Hash(Password),
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var service = CreateService(context, new FakeCacheService(), hasher, TestTimeProvider.Fixed());

            await service.AuthenticateAsync(new LoginRequest { strNombre = "Track", strPasswordPlano = Password });

            Assert.Empty(context.ChangeTracker.Entries());
        }

        private static LoginService CreateService(AppDbContext context, FakeCacheService cache, ISegUsuarioPasswordHasher hasher, TimeProvider clock)
        {
            return new LoginService(context, cache, hasher, new EfLoginLockoutStore(context, clock));
        }

        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private sealed class TestTimeProvider : TimeProvider
        {
            private DateTimeOffset _now;

            public TestTimeProvider(DateTimeOffset now)
            {
                _now = now;
            }

            public static TestTimeProvider Fixed()
            {
                return new TestTimeProvider(new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero));
            }

            public override DateTimeOffset GetUtcNow() => _now;

            public void Advance(TimeSpan delta) => _now += delta;
        }

        private sealed class RecordingHasher : ISegUsuarioPasswordHasher
        {
            private readonly FakeSegUsuarioPasswordHasher _inner = new();

            public readonly List<string> HashedInputs = new();

            public string Hash(string plano)
            {
                HashedInputs.Add(plano);
                return _inner.Hash(plano);
            }

            public bool Verify(string plano, string hash)
            {
                return _inner.Verify(plano, hash);
            }

            public bool NeedsRehash(string hash)
            {
                return _inner.NeedsRehash(hash);
            }
        }

        private sealed class FakeCacheService : ICacheService
        {
            private readonly Dictionary<string, object?> _store = new();

            public readonly List<string> Removed = new();

            public List<string> Keys => new(_store.Keys);

            public readonly List<TimeSpan> Ttls = new();

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
                Removed.Add(prefix + key);
                _store.Remove(prefix + key);
                return Task.CompletedTask;
            }
        }
    }
}
