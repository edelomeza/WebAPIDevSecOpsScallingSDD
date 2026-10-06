using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;
using UsuarioModel = WebAPIDevSecOpsScallingSDD.Models.SegUsuario;

namespace UnitTest.Login2Fa
{
    public class LoginRequires2FaTests
    {
        private const string Password = "Secreto123";

        [Fact]
        public async Task PasswordOkWith2faReturnsRequiresTwoFactorWithHexTemp()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Dos2fa",
                strCorreoElectronico = "dos2fa@test.local",
                strPWD = hasher.Hash(Password),
                bln2FAHabilitado = true,
                str2FASecreto = "JBSWY3DPEHPK3PXP",
            });
            await context.SaveChangesAsync();
            var service = new LoginService(context, cache, hasher);

            var result = await service.AuthenticateAsync(new LoginRequest { strNombre = "Dos2fa", strPasswordPlano = Password });

            Assert.Equal(LoginStatus.RequiresTwoFactor, result.Status);
            Assert.Null(result.Token);
            Assert.False(string.IsNullOrEmpty(result.TempCode));
            Assert.Equal(64, result.TempCode!.Length);
            Assert.Matches("^[0-9A-F]+$", result.TempCode);
            Assert.Equal("Dos2fa", await cache.GetAsync<string>("cache:", $"login2fa:{result.TempCode}"));
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(120), ttl));
        }

        [Fact]
        public async Task PasswordOkWithout2faStillReturnsTokenWithoutTemp()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Uno2fa",
                strCorreoElectronico = "uno2fa@test.local",
                strPWD = hasher.Hash(Password),
            });
            await context.SaveChangesAsync();
            var service = new LoginService(context, cache, hasher);

            var result = await service.AuthenticateAsync(new LoginRequest { strNombre = "Uno2fa", strPasswordPlano = Password });

            Assert.Equal(LoginStatus.Authenticated, result.Status);
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.Null(result.TempCode);
            Assert.DoesNotContain(cache.Keys, key => key.StartsWith("cache:login2fa:", StringComparison.Ordinal));
        }

        [Fact]
        public async Task RequiresTwoFactorClearsAttempts()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Tres2fa",
                strCorreoElectronico = "tres2fa@test.local",
                strPWD = hasher.Hash(Password),
                bln2FAHabilitado = true,
                str2FASecreto = "JBSWY3DPEHPK3PXP",
            });
            await context.SaveChangesAsync();
            var service = new LoginService(context, cache, hasher);

            await service.AuthenticateAsync(new LoginRequest { strNombre = "Tres2fa", strPasswordPlano = "Mala1" });
            var result = await service.AuthenticateAsync(new LoginRequest { strNombre = "Tres2fa", strPasswordPlano = Password });

            Assert.Equal(LoginStatus.RequiresTwoFactor, result.Status);
            Assert.Equal(0, await cache.GetAsync<int>("attempts:", "Tres2fa"));
        }

        [Fact]
        public async Task WrongPasswordOn2faUserReturnsInvalidWithoutTemp()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Cuatro2fa",
                strCorreoElectronico = "cuatro2fa@test.local",
                strPWD = hasher.Hash(Password),
                bln2FAHabilitado = true,
                str2FASecreto = "JBSWY3DPEHPK3PXP",
            });
            await context.SaveChangesAsync();
            var service = new LoginService(context, cache, hasher);

            var result = await service.AuthenticateAsync(new LoginRequest { strNombre = "Cuatro2fa", strPasswordPlano = "Mala1" });

            Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
            Assert.Null(result.TempCode);
            Assert.DoesNotContain(cache.Keys, key => key.StartsWith("cache:login2fa:", StringComparison.Ordinal));
        }

        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private sealed class FakeCacheService : ICacheService
        {
            private readonly Dictionary<string, object?> _store = new();

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
                _store.Remove(prefix + key);
                return Task.CompletedTask;
            }
        }
    }
}
