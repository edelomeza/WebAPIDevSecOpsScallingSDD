using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;
using ProductoModel = WebAPIDevSecOpsScallingSDD.Models.ProProducto;

namespace UnitTest.ProProducto
{
    public class ProProductoServiceTests
    {
        [Fact]
        public async Task CreatePersistsAndRotatesCacheVersion()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new ProProductoService(context, cache);

            var created = await service.CreateAsync(new ProProductoCreateDto
            {
                strNombreProducto = "Laptop",
                intNumeroExistencia = 5,
                decPrecio = 99.99m,
            });

            Assert.True(created.id > 0);
            Assert.Equal("Laptop", (await service.GetByIdAsync(created.id))?.strNombreProducto);
            Assert.Equal(1, await cache.GetAsync<int>("cache:", "producto:version"));
            Assert.Empty(cache.Removed);
        }

        [Fact]
        public async Task NullDtosThrowArgumentNull()
        {
            await using var context = CreateContext();
            var service = new ProProductoService(context, new FakeCacheService());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateAsync(1, null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.DeleteAsync(1, null!));
        }

        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            var cache = new FakeCacheService();
            using var context = CreateContext();

            Assert.Throws<ArgumentNullException>(() => new ProProductoService(null!, cache));
            Assert.Throws<ArgumentNullException>(() => new ProProductoService(context, null!));
        }

        [Fact]
        public async Task ReadsDoNotTrackEntities()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            context.ProProductos.Add(new ProductoModel
            {
                strNombreProducto = "Track",
                intNumeroExistencia = 1,
                decPrecio = 10.00m,
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var service = new ProProductoService(context, cache);

            await service.GetByIdAsync(1);
            await service.GetPagedAsync(1, 20);
            await service.SearchByNameAsync("Track", 1, 20);

            Assert.Empty(context.ChangeTracker.Entries());
        }

        [Fact]
        public async Task CacheKeysFollowNamingConvention()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new ProProductoService(context, cache);
            var created = await service.CreateAsync(new ProProductoCreateDto
            {
                strNombreProducto = "Key",
                intNumeroExistencia = 1,
                decPrecio = 10.00m,
            });
            await service.GetByIdAsync(created.id);
            await service.GetPagedAsync(1, 20);
            await service.SearchByNameAsync("Key", 1, 20);

            Assert.Contains(cache.Keys, key => key == "cache:producto:" + created.id);
            Assert.Contains(cache.Keys, key => key == "cache:producto:page:1:1:20");
            Assert.Contains(cache.Keys, key => key == "cache:producto:search:1:Key:1:20");
            Assert.Contains(cache.Keys, key => key == "cache:producto:version");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
        }

        [Fact]
        public async Task GetByIdServesFromCacheAfterDelete()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new ProProductoService(context, cache);
            var created = await service.CreateAsync(new ProProductoCreateDto
            {
                strNombreProducto = "Beto",
                intNumeroExistencia = 2,
                decPrecio = 20.00m,
            });

            var first = await service.GetByIdAsync(created.id);
            context.ProProductos.Remove(await context.ProProductos.SingleAsync(e => e.id == created.id));
            await context.SaveChangesAsync();
            var second = await service.GetByIdAsync(created.id);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(first.strNombreProducto, second.strNombreProducto);
        }

        [Fact]
        public async Task GetByIdReturnsNullWhenMissing()
        {
            using var context = CreateContext();
            var service = new ProProductoService(context, new FakeCacheService());

            Assert.Null(await service.GetByIdAsync(999));
        }

        [Fact]
        public async Task GetPagedReturnsOrderedPagesAndCaches()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new ProProductoService(context, cache);
            context.ProProductos.AddRange(
                new ProductoModel { id = 3, strNombreProducto = "C3", intNumeroExistencia = 3, decPrecio = 30.00m },
                new ProductoModel { id = 1, strNombreProducto = "C1", intNumeroExistencia = 1, decPrecio = 10.00m },
                new ProductoModel { id = 2, strNombreProducto = "C2", intNumeroExistencia = 2, decPrecio = 20.00m });
            await context.SaveChangesAsync();

            var page1 = await service.GetPagedAsync(1, 2);
            var page2 = await service.GetPagedAsync(2, 2);

            Assert.Equal(3, page1.TotalCount);
            Assert.Equal(2, page1.Items.Count);
            Assert.Equal(1, page1.Items[0].id);
            Assert.Equal("C1", page1.Items[0].strNombreProducto);
            Assert.Equal(2, page1.Items[1].id);
            Assert.Equal("C2", page1.Items[1].strNombreProducto);
            Assert.Single(page2.Items);
            Assert.Equal(3, page2.Items[0].id);
            Assert.Equal("C3", page2.Items[0].strNombreProducto);

            context.ProProductos.RemoveRange(context.ProProductos);
            await context.SaveChangesAsync();
            var cached = await service.GetPagedAsync(1, 2);
            Assert.Equal(3, cached.TotalCount);
            Assert.Equal("C1", cached.Items[0].strNombreProducto);
        }

        [Fact]
        public async Task UpdateSucceedsAndInvalidatesById()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new ProProductoService(context, cache);
            var created = await service.CreateAsync(new ProProductoCreateDto
            {
                strNombreProducto = "Dora",
                intNumeroExistencia = 1,
                decPrecio = 10.00m,
            });

            var updated = await service.UpdateAsync(created.id, new ProProductoUpdateDto
            {
                id = created.id,
                strNombreProducto = "Dora X",
                intNumeroExistencia = 7,
                decPrecio = 70.00m,
                RowVersion = created.RowVersion,
            });

            Assert.NotNull(updated);
            Assert.Equal("Dora X", updated.strNombreProducto);
            Assert.Equal(7, updated.intNumeroExistencia);
            Assert.Equal(70.00m, updated.decPrecio);
            Assert.Contains(cache.Removed, key => key == "cache:producto:" + created.id);
        }

        [Fact]
        public async Task UpdateReturnsNullWhenMissing()
        {
            using var context = CreateContext();
            var service = new ProProductoService(context, new FakeCacheService());

            var result = await service.UpdateAsync(999, new ProProductoUpdateDto
            {
                id = 999,
                strNombreProducto = "Nadie",
                intNumeroExistencia = 0,
                decPrecio = 0m,
                RowVersion = new byte[] { 1 },
            });

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateWithStaleRowVersionThrowsConflict()
        {
            await using var context = CreateContext();
            var service = new ProProductoService(context, new FakeCacheService());
            var created = await service.CreateAsync(new ProProductoCreateDto
            {
                strNombreProducto = "Eva",
                intNumeroExistencia = 1,
                decPrecio = 10.00m,
            });

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.UpdateAsync(created.id, new ProProductoUpdateDto
            {
                id = created.id,
                strNombreProducto = "Eva X",
                intNumeroExistencia = 1,
                decPrecio = 10.00m,
                RowVersion = new byte[] { 9 },
            }));
        }

        [Fact]
        public async Task DeleteRemovesAndReturnsTrue()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new ProProductoService(context, cache);
            var created = await service.CreateAsync(new ProProductoCreateDto
            {
                strNombreProducto = "Fin",
                intNumeroExistencia = 1,
                decPrecio = 10.00m,
            });

            var deleted = await service.DeleteAsync(created.id, new ProProductoDeleteDto { id = created.id, RowVersion = created.RowVersion });

            Assert.True(deleted);
            Assert.Null(await service.GetByIdAsync(created.id));
            Assert.Contains(cache.Removed, key => key == "cache:producto:" + created.id);
        }

        [Fact]
        public async Task DeleteReturnsFalseWhenMissing()
        {
            using var context = CreateContext();
            var service = new ProProductoService(context, new FakeCacheService());

            Assert.False(await service.DeleteAsync(999, new ProProductoDeleteDto { id = 999, RowVersion = new byte[] { 1 } }));
        }

        [Fact]
        public async Task DeleteWithStaleRowVersionThrowsConflict()
        {
            await using var context = CreateContext();
            var service = new ProProductoService(context, new FakeCacheService());
            var created = await service.CreateAsync(new ProProductoCreateDto
            {
                strNombreProducto = "Gus",
                intNumeroExistencia = 1,
                decPrecio = 10.00m,
            });

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.DeleteAsync(created.id, new ProProductoDeleteDto { id = created.id, RowVersion = new byte[] { 9 } }));
        }

        [Fact]
        public async Task SearchByNameReturnsPagedOrderedAndCaches()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new ProProductoService(context, cache);
            context.ProProductos.AddRange(
                new ProductoModel { strNombreProducto = "Tornillo A", intNumeroExistencia = 1, decPrecio = 10.00m },
                new ProductoModel { strNombreProducto = "Tornillo B", intNumeroExistencia = 2, decPrecio = 20.00m },
                new ProductoModel { strNombreProducto = "Martillo", intNumeroExistencia = 3, decPrecio = 30.00m });
            await context.SaveChangesAsync();

            var result = await service.SearchByNameAsync("Tornillo", 1, 20);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.True(result.Items[0].id < result.Items[1].id);
            Assert.All(result.Items, item => Assert.Contains("Tornillo", item.strNombreProducto, StringComparison.Ordinal));

            context.ProProductos.RemoveRange(context.ProProductos);
            await context.SaveChangesAsync();
            var cached = await service.SearchByNameAsync("Tornillo", 1, 20);
            Assert.Equal(2, cached.TotalCount);
        }

        [Fact]
        public async Task SearchByNamePaginatesSecondPage()
        {
            await using var context = CreateContext();
            var service = new ProProductoService(context, new FakeCacheService());
            context.ProProductos.AddRange(
                new ProductoModel { strNombreProducto = "Tornillo Uno", intNumeroExistencia = 1, decPrecio = 10.00m },
                new ProductoModel { strNombreProducto = "Tornillo Dos", intNumeroExistencia = 2, decPrecio = 20.00m });
            await context.SaveChangesAsync();

            var page2 = await service.SearchByNameAsync("Tornillo", 2, 2);

            Assert.Equal(2, page2.TotalCount);
            Assert.Empty(page2.Items);
        }

        [Fact]
        public async Task SearchByNameTrimsTexto()
        {
            await using var context = CreateContext();
            var service = new ProProductoService(context, new FakeCacheService());
            context.ProProductos.Add(new ProductoModel { strNombreProducto = "Tornillo", intNumeroExistencia = 1, decPrecio = 10.00m });
            await context.SaveChangesAsync();

            var result = await service.SearchByNameAsync("  Tornillo  ", 1, 20);

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Tornillo", result.Items[0].strNombreProducto);
        }

        [Fact]
        public async Task SearchByNameEmptyReturnsEmptyWithoutQueryingCache()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new ProProductoService(context, cache);

            var blank = await service.SearchByNameAsync("   ", 1, 20);
            var nulled = await service.SearchByNameAsync(null!, 2, 5);

            Assert.Equal(0, blank.TotalCount);
            Assert.Empty(blank.Items);
            Assert.Equal(1, blank.Page);
            Assert.Equal(0, nulled.TotalCount);
            Assert.Empty(nulled.Items);
            Assert.Empty(cache.Keys);
        }

        [Fact]
        public async Task SearchByNameWithoutMatchesReturnsEmptyPage()
        {
            await using var context = CreateContext();
            var service = new ProProductoService(context, new FakeCacheService());
            context.ProProductos.Add(new ProductoModel { strNombreProducto = "Tornillo", intNumeroExistencia = 1, decPrecio = 10.00m });
            await context.SaveChangesAsync();

            var result = await service.SearchByNameAsync("Inexistente", 1, 20);

            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
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
