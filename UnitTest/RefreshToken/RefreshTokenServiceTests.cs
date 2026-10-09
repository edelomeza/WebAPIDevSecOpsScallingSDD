using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Services;

namespace UnitTest.RefreshToken
{
    public class RefreshTokenServiceTests
    {
        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            using var context = CreateContext();
            var cache = new FakeCacheService();

            Assert.Throws<ArgumentNullException>(() => new RefreshTokenService(null!, cache, CreateJwt()));
            Assert.Throws<ArgumentNullException>(() => new RefreshTokenService(context, null!, CreateJwt()));
            Assert.Throws<ArgumentNullException>(() => new RefreshTokenService(context, cache, null!));
        }

        [Fact]
        public async Task CreateReturnsJwtAccessAndHashedRefresh()
        {
            await using var context = CreateContext();
            var service = new RefreshTokenService(context, new FakeCacheService(), CreateJwt());

            var pair = await service.CreateAsync(7);

            Assert.Equal(3, pair.Token.Split('.').Length);
            Assert.Equal(64, pair.RefreshToken.Length);
            Assert.NotEqual(pair.Token, pair.RefreshToken);
            var row = await context.SegRefreshTokens.SingleAsync();
            Assert.Equal(7, row.idSegUsuario);
            Assert.Equal(64, row.strTokenHash.Length);
            Assert.DoesNotContain(pair.RefreshToken, row.strTokenHash, StringComparison.Ordinal);
            Assert.DoesNotContain(pair.Token, row.strTokenHash, StringComparison.Ordinal);
            Assert.Null(row.dteRevokedAt);
            Assert.True(row.dteExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task RotateEmitsNewAndRevokesOldWithReplacementLink()
        {
            await using var context = CreateContext();
            var service = new RefreshTokenService(context, new FakeCacheService(), CreateJwt());
            var pair = await service.CreateAsync(7);

            var rotated = await service.RotateAsync(pair.RefreshToken);

            Assert.Equal(RefreshStatus.Rotated, rotated.Status);
            Assert.False(string.IsNullOrEmpty(rotated.Token));
            Assert.False(string.IsNullOrEmpty(rotated.RefreshToken));
            Assert.NotEqual(pair.RefreshToken, rotated.RefreshToken);
            Assert.Equal(2, await context.SegRefreshTokens.CountAsync());
            var oldHash = RefreshTokenService.ComputeHash(pair.RefreshToken);
            var oldRow = await context.SegRefreshTokens.SingleAsync(e => e.strTokenHash == oldHash);
            Assert.NotNull(oldRow.dteRevokedAt);
            Assert.Equal(RefreshTokenService.ComputeHash(rotated.RefreshToken!), oldRow.strReplacedByTokenHash);
        }

        [Fact]
        public async Task ReusedTokenReturnsInvalid()
        {
            await using var context = CreateContext();
            var service = new RefreshTokenService(context, new FakeCacheService(), CreateJwt());
            var pair = await service.CreateAsync(7);
            var rotated = await service.RotateAsync(pair.RefreshToken);
            Assert.Equal(RefreshStatus.Rotated, rotated.Status);

            var reuseOld = await service.RotateAsync(pair.RefreshToken);
            var reuseOldAgain = await service.RotateAsync(pair.RefreshToken);

            Assert.Equal(RefreshStatus.Invalid, reuseOld.Status);
            Assert.Null(reuseOld.Token);
            Assert.Equal(RefreshStatus.Invalid, reuseOldAgain.Status);
            Assert.Equal(2, await context.SegRefreshTokens.CountAsync());
        }

        [Fact]
        public async Task UnknownExpiredAndNonHexReturnInvalid()
        {
            await using var context = CreateContext();
            var service = new RefreshTokenService(context, new FakeCacheService(), CreateJwt());
            var pair = await service.CreateAsync(7);

            var unknown = await service.RotateAsync(new string('A', 64));
            var nonHex = await service.RotateAsync("not-hex!!");
            var blank = await service.RotateAsync("   ");
            var nulled = await service.RotateAsync(null);

            Assert.Equal(RefreshStatus.Invalid, unknown.Status);
            Assert.Equal(RefreshStatus.Invalid, nonHex.Status);
            Assert.Equal(RefreshStatus.Invalid, blank.Status);
            Assert.Equal(RefreshStatus.Invalid, nulled.Status);

            var row = await context.SegRefreshTokens.SingleAsync();
            row.dteExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await context.SaveChangesAsync();
            var expired = await service.RotateAsync(pair.RefreshToken);
            Assert.Equal(RefreshStatus.Invalid, expired.Status);
        }

        [Fact]
        public async Task RevokeMarksRowOnce()
        {
            await using var context = CreateContext();
            var service = new RefreshTokenService(context, new FakeCacheService(), CreateJwt());
            var pair = await service.CreateAsync(7);

            Assert.True(await service.RevokeAsync(pair.RefreshToken));
            Assert.False(await service.RevokeAsync(pair.RefreshToken));
            Assert.False(await service.RevokeAsync(null));
            var row = await context.SegRefreshTokens.SingleAsync();
            Assert.NotNull(row.dteRevokedAt);
        }

        [Fact]
        public async Task LogoutRevokesAndBlacklistsFallbackHash()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new RefreshTokenService(context, cache, CreateJwt());
            var pair = await service.CreateAsync(7);

            await service.LogoutAsync(pair.RefreshToken, null);

            var expectedJti = RefreshTokenService.ComputeHash(pair.RefreshToken);
            Assert.Equal("revoked", await cache.GetAsync<string>("blacklist:", expectedJti));
            Assert.Contains(cache.Keys, key => key == "blacklist:" + expectedJti);
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(120), ttl));
            var row = await context.SegRefreshTokens.SingleAsync();
            Assert.NotNull(row.dteRevokedAt);
        }

        [Fact]
        public async Task LogoutWithExplicitJtiUsesClaim()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new RefreshTokenService(context, cache, CreateJwt());
            var pair = await service.CreateAsync(7);

            await service.LogoutAsync(pair.RefreshToken, "claim-jti-1");

            Assert.Equal("revoked", await cache.GetAsync<string>("blacklist:", "claim-jti-1"));
        }

        [Fact]
        public async Task LogoutWithoutValuesDoesNotThrowNorCache()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new RefreshTokenService(context, cache, CreateJwt());

            await service.LogoutAsync(null, null);
            await service.LogoutAsync("   ", "  ");

            Assert.Empty(cache.Keys);
        }

        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static JwtTokenService CreateJwt(string? key = null)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = key ?? new string('K', 32),
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:Audience"] = "test-audience",
                })
                .Build();
            return new JwtTokenService(configuration);
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
