using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;
using EstadoModel = WebAPIDevSecOpsScallingSDD.Models.VenCatEstado;

namespace UnitTest.VenCatEstado
{
    public class VenCatEstadoServiceTests
    {
        [Fact]
        public async Task CreatePersistsAndRotatesCacheVersion()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new VenCatEstadoService(context, cache);

            var created = await service.CreateAsync(new VenCatEstadoCreateDto
            {
                strValor = "Vigente",
                strDescripcion = "Venta vigente",
            });

            Assert.True(created.id > 0);
            Assert.Equal("Vigente", (await service.GetByIdAsync(created.id))?.strValor);
            Assert.Equal(1, await cache.GetAsync<int>("cache:", "estado-venta:version"));
            Assert.Empty(cache.Removed);
        }

        [Fact]
        public async Task NullDtosThrowArgumentNull()
        {
            await using var context = CreateContext();
            var service = new VenCatEstadoService(context, new FakeCacheService());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateAsync(1, null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.DeleteAsync(1, null!));
        }

        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            var cache = new FakeCacheService();
            using var context = CreateContext();

            Assert.Throws<ArgumentNullException>(() => new VenCatEstadoService(null!, cache));
            Assert.Throws<ArgumentNullException>(() => new VenCatEstadoService(context, null!));
        }

        [Fact]
        public async Task ReadsDoNotTrackEntities()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            context.VenCatEstados.Add(new EstadoModel { strValor = "Track" });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var service = new VenCatEstadoService(context, cache);

            await service.GetByIdAsync(1);
            await service.GetPagedAsync(1, 20);

            Assert.Empty(context.ChangeTracker.Entries());
        }

        [Fact]
        public async Task CacheKeysFollowNamingConvention()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new VenCatEstadoService(context, cache);
            var created = await service.CreateAsync(new VenCatEstadoCreateDto { strValor = "Key" });
            await service.GetByIdAsync(created.id);
            await service.GetPagedAsync(1, 20);

            Assert.Contains(cache.Keys, key => key == "cache:estado-venta:" + created.id);
            Assert.Contains(cache.Keys, key => key == "cache:estado-venta:page:1:1:20");
            Assert.Contains(cache.Keys, key => key == "cache:estado-venta:version");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
        }

        [Fact]
        public async Task GetByIdServesFromCacheAfterDelete()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new VenCatEstadoService(context, cache);
            var created = await service.CreateAsync(new VenCatEstadoCreateDto { strValor = "Beto" });

            var first = await service.GetByIdAsync(created.id);
            context.VenCatEstados.Remove(await context.VenCatEstados.SingleAsync(e => e.id == created.id));
            await context.SaveChangesAsync();
            var second = await service.GetByIdAsync(created.id);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(first.strValor, second.strValor);
        }

        [Fact]
        public async Task GetByIdReturnsNullWhenMissing()
        {
            using var context = CreateContext();
            var service = new VenCatEstadoService(context, new FakeCacheService());

            Assert.Null(await service.GetByIdAsync(999));
        }

        [Fact]
        public async Task GetPagedReturnsOrderedPagesAndCaches()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new VenCatEstadoService(context, cache);
            context.VenCatEstados.AddRange(
                new EstadoModel { id = 3, strValor = "C3" },
                new EstadoModel { id = 1, strValor = "C1" },
                new EstadoModel { id = 2, strValor = "C2" });
            await context.SaveChangesAsync();

            var page1 = await service.GetPagedAsync(1, 2);
            var page2 = await service.GetPagedAsync(2, 2);

            Assert.Equal(3, page1.TotalCount);
            Assert.Equal(2, page1.Items.Count);
            Assert.Equal(1, page1.Items[0].id);
            Assert.Equal("C1", page1.Items[0].strValor);
            Assert.Equal(2, page1.Items[1].id);
            Assert.Equal("C2", page1.Items[1].strValor);
            Assert.Single(page2.Items);
            Assert.Equal(3, page2.Items[0].id);
            Assert.Equal("C3", page2.Items[0].strValor);

            context.VenCatEstados.RemoveRange(context.VenCatEstados);
            await context.SaveChangesAsync();
            var cached = await service.GetPagedAsync(1, 2);
            Assert.Equal(3, cached.TotalCount);
            Assert.Equal("C1", cached.Items[0].strValor);
        }

        [Fact]
        public async Task UpdateSucceedsAndInvalidatesById()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new VenCatEstadoService(context, cache);
            var created = await service.CreateAsync(new VenCatEstadoCreateDto { strValor = "Dora" });

            var updated = await service.UpdateAsync(created.id, new VenCatEstadoUpdateDto
            {
                id = created.id,
                strValor = "Dora X",
                strDescripcion = "Actualizada",
            });

            Assert.NotNull(updated);
            Assert.Equal("Dora X", updated.strValor);
            Assert.Equal("Actualizada", updated.strDescripcion);
            Assert.Contains(cache.Removed, key => key == "cache:estado-venta:" + created.id);
        }

        [Fact]
        public async Task UpdateReturnsNullWhenMissing()
        {
            using var context = CreateContext();
            var service = new VenCatEstadoService(context, new FakeCacheService());

            var result = await service.UpdateAsync(999, new VenCatEstadoUpdateDto { id = 999, strValor = "Nadie" });

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdatePersistsAcrossContexts()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateNamedContext(dbName);
            var service = new VenCatEstadoService(context, new FakeCacheService());
            var created = await service.CreateAsync(new VenCatEstadoCreateDto { strValor = "Dora" });

            await service.UpdateAsync(created.id, new VenCatEstadoUpdateDto { id = created.id, strValor = "Dora X" });

            await using var fresh = CreateNamedContext(dbName);
            var freshService = new VenCatEstadoService(fresh, new FakeCacheService());
            Assert.Equal("Dora X", (await freshService.GetByIdAsync(created.id))?.strValor);
        }

        [Fact]
        public async Task DeleteRemovesAndReturnsTrue()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new VenCatEstadoService(context, cache);
            var created = await service.CreateAsync(new VenCatEstadoCreateDto { strValor = "Fin" });

            var deleted = await service.DeleteAsync(created.id, new VenCatEstadoDeleteDto { id = created.id });

            Assert.True(deleted);
            Assert.Null(await service.GetByIdAsync(created.id));
            Assert.Contains(cache.Removed, key => key == "cache:estado-venta:" + created.id);
        }

        [Fact]
        public async Task DeleteReturnsFalseWhenMissing()
        {
            using var context = CreateContext();
            var service = new VenCatEstadoService(context, new FakeCacheService());

            Assert.False(await service.DeleteAsync(999, new VenCatEstadoDeleteDto { id = 999 }));
        }

        private static AppDbContext CreateContext() => CreateNamedContext(Guid.NewGuid().ToString());

        private static AppDbContext CreateNamedContext(string name)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new AppDbContext(options);
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
