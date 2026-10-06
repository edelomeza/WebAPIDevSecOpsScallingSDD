using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;
using ClienteModel = WebAPIDevSecOpsScallingSDD.Models.CliCliente;

namespace UnitTest.CliCliente
{
    public class CliClienteServiceTests
    {
        [Fact]
        public async Task CreatePersistsAndRotatesCacheVersion()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new CliClienteService(context, cache);

            var created = await service.CreateAsync(new CliClienteCreateDto
            {
                strNombreCliente = "Ana",
                strCorreoElectronico = "ana@test.local",
                strNumeroTelefono = "5550000001",
            });

            Assert.True(created.id > 0);
            Assert.Equal("Ana", (await service.GetByIdAsync(created.id))?.strNombreCliente);
            Assert.Equal(1, await cache.GetAsync<int>("cache:", "cliente:version"));
            Assert.Empty(cache.Removed);
        }

        [Fact]
        public async Task NullDtosThrowArgumentNull()
        {
            await using var context = CreateContext();
            var service = new CliClienteService(context, new FakeCacheService());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateAsync(1, null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.DeleteAsync(1, null!));
        }

        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            var cache = new FakeCacheService();
            using var context = CreateContext();

            Assert.Throws<ArgumentNullException>(() => new CliClienteService(null!, cache));
            Assert.Throws<ArgumentNullException>(() => new CliClienteService(context, null!));
        }

        [Fact]
        public async Task ReadsDoNotTrackEntities()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            context.CliClientes.Add(new ClienteModel
            {
                strNombreCliente = "Track",
                strCorreoElectronico = "track@test.local",
                strNumeroTelefono = "5550000011",
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var service = new CliClienteService(context, cache);

            await service.GetByIdAsync(1);
            await service.GetPagedAsync(1, 20);
            await service.SearchByNameAsync("Track", 1, 20);
            await service.AutocompleteAsync("Track", 10);

            Assert.Empty(context.ChangeTracker.Entries());
        }

        [Fact]
        public async Task CacheKeysFollowNamingConvention()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new CliClienteService(context, cache);
            var created = await service.CreateAsync(new CliClienteCreateDto
            {
                strNombreCliente = "Key",
                strCorreoElectronico = "key@test.local",
                strNumeroTelefono = "5550000012",
            });
            await service.GetByIdAsync(created.id);
            await service.GetPagedAsync(1, 20);
            await service.SearchByNameAsync("Key", 1, 20);
            await service.AutocompleteAsync("Key", 10);

            Assert.Contains(cache.Keys, key => key == "cache:cliente:" + created.id);
            Assert.Contains(cache.Keys, key => key == "cache:cliente:page:1:1:20");
            Assert.Contains(cache.Keys, key => key == "cache:cliente:search:1:Key:1:20");
            Assert.Contains(cache.Keys, key => key == "cache:cliente:autocomplete:1:Key:10");
            Assert.Contains(cache.Keys, key => key == "cache:cliente:version");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
        }

        [Fact]
        public async Task GetByIdServesFromCacheAfterDelete()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new CliClienteService(context, cache);
            var created = await service.CreateAsync(new CliClienteCreateDto
            {
                strNombreCliente = "Beto",
                strCorreoElectronico = "beto@test.local",
                strNumeroTelefono = "5550000002",
            });

            var first = await service.GetByIdAsync(created.id);
            context.CliClientes.Remove(await context.CliClientes.SingleAsync(e => e.id == created.id));
            await context.SaveChangesAsync();
            var second = await service.GetByIdAsync(created.id);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(first.strNombreCliente, second.strNombreCliente);
        }

        [Fact]
        public async Task GetByIdReturnsNullWhenMissing()
        {
            using var context = CreateContext();
            var service = new CliClienteService(context, new FakeCacheService());

            Assert.Null(await service.GetByIdAsync(999));
        }

        [Fact]
        public async Task GetPagedReturnsOrderedPagesAndCaches()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new CliClienteService(context, cache);
            context.CliClientes.AddRange(
                new ClienteModel { id = 3, strNombreCliente = "C3", strCorreoElectronico = "c3@test.local", strNumeroTelefono = "5550000003" },
                new ClienteModel { id = 1, strNombreCliente = "C1", strCorreoElectronico = "c1@test.local", strNumeroTelefono = "5550000001" },
                new ClienteModel { id = 2, strNombreCliente = "C2", strCorreoElectronico = "c2@test.local", strNumeroTelefono = "5550000002" });
            await context.SaveChangesAsync();

            var page1 = await service.GetPagedAsync(1, 2);
            var page2 = await service.GetPagedAsync(2, 2);

            Assert.Equal(3, page1.TotalCount);
            Assert.Equal(2, page1.Items.Count);
            Assert.Equal(1, page1.Items[0].id);
            Assert.Equal("C1", page1.Items[0].strNombreCliente);
            Assert.Equal(2, page1.Items[1].id);
            Assert.Equal("C2", page1.Items[1].strNombreCliente);
            Assert.Single(page2.Items);
            Assert.Equal(3, page2.Items[0].id);
            Assert.Equal("C3", page2.Items[0].strNombreCliente);

            context.CliClientes.RemoveRange(context.CliClientes);
            await context.SaveChangesAsync();
            var cached = await service.GetPagedAsync(1, 2);
            Assert.Equal(3, cached.TotalCount);
            Assert.Equal("C1", cached.Items[0].strNombreCliente);
        }

        [Fact]
        public async Task UpdateSucceedsAndInvalidatesById()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new CliClienteService(context, cache);
            var created = await service.CreateAsync(new CliClienteCreateDto
            {
                strNombreCliente = "Dora",
                strCorreoElectronico = "dora@test.local",
                strNumeroTelefono = "5550000004",
            });

            var updated = await service.UpdateAsync(created.id, new CliClienteUpdateDto
            {
                id = created.id,
                strNombreCliente = "Dora X",
                strCorreoElectronico = "dora@test.local",
                strNumeroTelefono = "5550000004",
                RowVersion = created.RowVersion,
            });

            Assert.NotNull(updated);
            Assert.Equal("Dora X", updated.strNombreCliente);
            Assert.Contains(cache.Removed, key => key == "cache:cliente:" + created.id);
        }

        [Fact]
        public async Task UpdateReturnsNullWhenMissing()
        {
            using var context = CreateContext();
            var service = new CliClienteService(context, new FakeCacheService());

            var result = await service.UpdateAsync(999, new CliClienteUpdateDto
            {
                id = 999,
                strNombreCliente = "Nadie",
                strCorreoElectronico = "nadie@test.local",
                strNumeroTelefono = "5550000000",
                RowVersion = new byte[] { 1 },
            });

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateWithStaleRowVersionThrowsConflict()
        {
            await using var context = CreateContext();
            var service = new CliClienteService(context, new FakeCacheService());
            var created = await service.CreateAsync(new CliClienteCreateDto
            {
                strNombreCliente = "Eva",
                strCorreoElectronico = "eva@test.local",
                strNumeroTelefono = "5550000005",
            });

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.UpdateAsync(created.id, new CliClienteUpdateDto
            {
                id = created.id,
                strNombreCliente = "Eva X",
                strCorreoElectronico = "eva@test.local",
                strNumeroTelefono = "5550000005",
                RowVersion = new byte[] { 9 },
            }));
        }

        [Fact]
        public async Task DeleteRemovesAndReturnsTrue()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new CliClienteService(context, cache);
            var created = await service.CreateAsync(new CliClienteCreateDto
            {
                strNombreCliente = "Fin",
                strCorreoElectronico = "fin@test.local",
                strNumeroTelefono = "5550000006",
            });

            var deleted = await service.DeleteAsync(created.id, new CliClienteDeleteDto { id = created.id, RowVersion = created.RowVersion });

            Assert.True(deleted);
            Assert.Null(await service.GetByIdAsync(created.id));
            Assert.Contains(cache.Removed, key => key == "cache:cliente:" + created.id);
        }

        [Fact]
        public async Task DeleteReturnsFalseWhenMissing()
        {
            using var context = CreateContext();
            var service = new CliClienteService(context, new FakeCacheService());

            Assert.False(await service.DeleteAsync(999, new CliClienteDeleteDto { id = 999, RowVersion = new byte[] { 1 } }));
        }

        [Fact]
        public async Task DeleteWithStaleRowVersionThrowsConflict()
        {
            await using var context = CreateContext();
            var service = new CliClienteService(context, new FakeCacheService());
            var created = await service.CreateAsync(new CliClienteCreateDto
            {
                strNombreCliente = "Gus",
                strCorreoElectronico = "gus@test.local",
                strNumeroTelefono = "5550000007",
            });

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.DeleteAsync(created.id, new CliClienteDeleteDto { id = created.id, RowVersion = new byte[] { 9 } }));
        }

        [Fact]
        public async Task SearchByNameReturnsPagedOrderedAndCaches()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new CliClienteService(context, cache);
            context.CliClientes.AddRange(
                new ClienteModel { strNombreCliente = "Ana Paula", strCorreoElectronico = "a1@test.local", strNumeroTelefono = "5550000021" },
                new ClienteModel { strNombreCliente = "Ana Maria", strCorreoElectronico = "a2@test.local", strNumeroTelefono = "5550000022" },
                new ClienteModel { strNombreCliente = "Pedro", strCorreoElectronico = "p@test.local", strNumeroTelefono = "5550000023" });
            await context.SaveChangesAsync();

            var result = await service.SearchByNameAsync("Ana", 1, 20);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.True(result.Items[0].id < result.Items[1].id);
            Assert.All(result.Items, item => Assert.Contains("Ana", item.strNombreCliente, StringComparison.Ordinal));

            var page2 = await service.SearchByNameAsync("Ana", 2, 2);
            Assert.Equal(2, page2.TotalCount);
            Assert.Empty(page2.Items);

            context.CliClientes.RemoveRange(context.CliClientes);
            await context.SaveChangesAsync();
            var cached = await service.SearchByNameAsync("Ana", 1, 20);
            Assert.Equal(2, cached.TotalCount);
        }

        [Fact]
        public async Task SearchByNameTrimsTexto()
        {
            await using var context = CreateContext();
            var service = new CliClienteService(context, new FakeCacheService());
            context.CliClientes.Add(new ClienteModel { strNombreCliente = "Ana", strCorreoElectronico = "a@test.local", strNumeroTelefono = "5550000024" });
            await context.SaveChangesAsync();

            var result = await service.SearchByNameAsync("  Ana  ", 1, 20);

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Ana", result.Items[0].strNombreCliente);
        }

        [Fact]
        public async Task SearchByNameEmptyReturnsEmptyWithoutQueryingCache()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new CliClienteService(context, cache);

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
        public async Task AutocompleteReturnsTopNOrderedWithMinimalShape()
        {
            await using var context = CreateContext();
            var service = new CliClienteService(context, new FakeCacheService());
            context.CliClientes.AddRange(
                new ClienteModel { id = 3, strNombreCliente = "Ana C", strCorreoElectronico = "c@test.local", strNumeroTelefono = "5550000033" },
                new ClienteModel { id = 1, strNombreCliente = "Ana A", strCorreoElectronico = "a@test.local", strNumeroTelefono = "5550000031" },
                new ClienteModel { id = 2, strNombreCliente = "Ana B", strCorreoElectronico = "b@test.local", strNumeroTelefono = "5550000032" });
            await context.SaveChangesAsync();

            var top2 = await service.AutocompleteAsync("Ana", 2);

            Assert.Equal(2, top2.Count);
            Assert.Equal(1, top2[0].id);
            Assert.Equal("Ana A", top2[0].strNombreCliente);
            Assert.Equal(2, top2[1].id);

            var json = JsonSerializer.Serialize(top2);
            Assert.DoesNotContain("strCorreo", json, StringComparison.Ordinal);
            Assert.DoesNotContain("strNumeroTelefono", json, StringComparison.Ordinal);
            Assert.DoesNotContain("RowVersion", json, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AutocompleteTrimsTexto()
        {
            await using var context = CreateContext();
            var service = new CliClienteService(context, new FakeCacheService());
            context.CliClientes.Add(new ClienteModel { strNombreCliente = "Luis", strCorreoElectronico = "l@test.local", strNumeroTelefono = "5550000034" });
            await context.SaveChangesAsync();

            var result = await service.AutocompleteAsync("  Luis  ", 10);

            Assert.Single(result);
            Assert.Equal("Luis", result[0].strNombreCliente);
        }

        [Fact]
        public async Task AutocompleteNormalizesOutOfRangeToTen()
        {
            await using var context = CreateContext();
            var service = new CliClienteService(context, new FakeCacheService());
            for (var i = 0; i < 12; i++)
            {
                context.CliClientes.Add(new ClienteModel { strNombreCliente = $"Anexo{i}", strCorreoElectronico = $"a{i}@test.local", strNumeroTelefono = "5550000040" });
            }

            await context.SaveChangesAsync();

            var zero = await service.AutocompleteAsync("Anexo", 0);
            var over = await service.AutocompleteAsync("Anexo", 51);
            var negative = await service.AutocompleteAsync("Anexo", -5);
            var one = await service.AutocompleteAsync("Anexo", 1);

            Assert.Equal(10, zero.Count);
            Assert.Equal(10, over.Count);
            Assert.Equal(10, negative.Count);
            Assert.Single(one);
        }

        [Fact]
        public async Task AutocompleteBoundaryFiftyIsNotNormalized()
        {
            await using var context = CreateContext();
            var service = new CliClienteService(context, new FakeCacheService());
            for (var i = 0; i < 12; i++)
            {
                context.CliClientes.Add(new ClienteModel { strNombreCliente = $"Borde{i}", strCorreoElectronico = $"b{i}@test.local", strNumeroTelefono = "5550000050" });
            }

            await context.SaveChangesAsync();

            var fifty = await service.AutocompleteAsync("Borde", 50);

            Assert.Equal(12, fifty.Count);
        }

        [Fact]
        public async Task AutocompleteEmptyReturnsEmptyWithoutQueryingCache()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new CliClienteService(context, cache);

            var blank = await service.AutocompleteAsync("   ", 10);
            var nulled = await service.AutocompleteAsync(null!, 10);

            Assert.Empty(blank);
            Assert.Empty(nulled);
            Assert.Empty(cache.Keys);
        }

        [Fact]
        public async Task AutocompleteCachesResults()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new CliClienteService(context, cache);
            context.CliClientes.Add(new ClienteModel { strNombreCliente = "CacheMe", strCorreoElectronico = "c@test.local", strNumeroTelefono = "5550000060" });
            await context.SaveChangesAsync();

            var first = await service.AutocompleteAsync("CacheMe", 10);
            context.CliClientes.RemoveRange(context.CliClientes);
            await context.SaveChangesAsync();
            var second = await service.AutocompleteAsync("CacheMe", 10);

            Assert.Single(first);
            Assert.Single(second);
            Assert.Equal(first[0].strNombreCliente, second[0].strNombreCliente);
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
