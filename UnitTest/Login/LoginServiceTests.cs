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
            var service = new LoginService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.AuthenticateAsync(null!));
        }

        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            using var context = CreateContext();

            Assert.Throws<ArgumentNullException>(() => new LoginService(null!, cache, hasher));
            Assert.Throws<ArgumentNullException>(() => new LoginService(context, null!, hasher));
            Assert.Throws<ArgumentNullException>(() => new LoginService(context, cache, null!));
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
            var service = new LoginService(context, cache, hasher);

            await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = "Mala1" });
            await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = "Mala2" });
            var result = await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = Password });

            Assert.Equal(LoginStatus.Authenticated, result.Status);
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.Equal(0, await cache.GetAsync<int>("attempts:", "Ana"));
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
            var service = new LoginService(context, new FakeCacheService(), hasher);

            var unknown = await service.AuthenticateAsync(new LoginRequest { strNombre = "Nadie", strPasswordPlano = Password });
            var wrong = await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = "Mala1" });

            Assert.Equal(LoginStatus.InvalidCredentials, unknown.Status);
            Assert.Equal(LoginStatus.InvalidCredentials, wrong.Status);
            Assert.Null(unknown.Token);
            Assert.Null(wrong.Token);
        }

        [Fact]
        public async Task BlankCredentialsReturnInvalidWithoutQueryingCache()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new LoginService(context, cache, new FakeSegUsuarioPasswordHasher());

            var blankName = await service.AuthenticateAsync(new LoginRequest { strNombre = "   ", strPasswordPlano = Password });
            var blankPassword = await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = string.Empty });

            Assert.Equal(LoginStatus.InvalidCredentials, blankName.Status);
            Assert.Equal(LoginStatus.InvalidCredentials, blankPassword.Status);
            Assert.Empty(cache.Keys);
        }

        [Fact]
        public async Task FiveFailuresThenLockedOut()
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
            var service = new LoginService(context, cache, hasher);

            for (var i = 0; i < 5; i++)
            {
                var failure = await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = "Mala" });
                Assert.Equal(LoginStatus.InvalidCredentials, failure.Status);
            }

            var locked = await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = Password });

            Assert.Equal(LoginStatus.LockedOut, locked.Status);
            Assert.Null(locked.Token);
            Assert.Contains(cache.Keys, key => key == "lockout:Ana");
            Assert.Equal(0, await cache.GetAsync<int>("attempts:", "Ana"));
        }

        [Fact]
        public async Task UnknownUserLocksAfterFiveFailures()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new LoginService(context, cache, new FakeSegUsuarioPasswordHasher());

            for (var i = 0; i < 5; i++)
            {
                var failure = await service.AuthenticateAsync(new LoginRequest { strNombre = "Fantasma", strPasswordPlano = "Mala" });
                Assert.Equal(LoginStatus.InvalidCredentials, failure.Status);
            }

            var locked = await service.AuthenticateAsync(new LoginRequest { strNombre = "Fantasma", strPasswordPlano = "Mala" });

            Assert.Equal(LoginStatus.LockedOut, locked.Status);
        }

        [Fact]
        public async Task NullCredentialsReturnInvalidWithoutQueryingCache()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new LoginService(context, cache, new FakeSegUsuarioPasswordHasher());

            var nullName = await service.AuthenticateAsync(new LoginRequest { strNombre = null!, strPasswordPlano = Password });
            var nullPassword = await service.AuthenticateAsync(new LoginRequest { strNombre = "Ana", strPasswordPlano = null! });

            Assert.Equal(LoginStatus.InvalidCredentials, nullName.Status);
            Assert.Equal(LoginStatus.InvalidCredentials, nullPassword.Status);
            Assert.Empty(cache.Keys);
        }

        [Fact]
        public void CtorComputesDummyHashFromFixedSeed()
        {
            using var context = CreateContext();
            var hasher = new RecordingHasher();

            _ = new LoginService(context, new FakeCacheService(), hasher);

            Assert.Single(hasher.HashedInputs);
            Assert.Equal("login-anti-enumeration-dummy", hasher.HashedInputs[0]);
        }

        [Fact]
        public async Task LockoutUsesExpectedKeysAndTtl()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Key",
                strCorreoElectronico = "key@test.local",
                strPWD = hasher.Hash(Password),
            });
            await context.SaveChangesAsync();
            var service = new LoginService(context, cache, hasher);

            await service.AuthenticateAsync(new LoginRequest { strNombre = "Key", strPasswordPlano = "Mala" });

            Assert.Contains(cache.Keys, key => key == "attempts:Key");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(120), ttl));
        }

        [Fact]
        public async Task ReadsDoNotTrackEntities()
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
            var service = new LoginService(context, new FakeCacheService(), hasher);

            await service.AuthenticateAsync(new LoginRequest { strNombre = "Track", strPasswordPlano = Password });
            await service.AuthenticateAsync(new LoginRequest { strNombre = "Track", strPasswordPlano = "Mala" });

            Assert.Empty(context.ChangeTracker.Entries());
        }

        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
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
