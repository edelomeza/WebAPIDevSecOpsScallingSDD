using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;
using UsuarioModel = WebAPIDevSecOpsScallingSDD.Models.SegUsuario;

namespace UnitTest.SegUsuario
{
    public class SegUsuarioServiceTests
    {
        [Fact]
        public async Task CreatePersistsHashesAndRotatesCacheVersion()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, cache, new FakeSegUsuarioPasswordHasher());

            var created = await service.CreateAsync(new SegUsuarioCreateDto
            {
                strNombre = "Ana",
                strCorreoElectronico = "ana@example.com",
                strPasswordPlano = "Secreto123",
            });

            Assert.True(created.id > 0);
            Assert.Equal("Ana", created.strNombre);
            Assert.Equal("ana@example.com", created.strCorreoElectronico);
            Assert.False(created.bln2FAHabilitado);
            Assert.NotNull(created.dteFechaRegistro);
            Assert.Equal("Ana", (await service.GetByIdAsync(created.id))?.strNombre);
            Assert.Equal(1, await cache.GetAsync<int>("cache:", "usuario:version"));
            Assert.Empty(cache.Removed);

            var entity = await context.SegUsuarios.SingleAsync(e => e.id == created.id);
            Assert.NotEqual("Secreto123", entity.strPWD);
            Assert.True(new FakeSegUsuarioPasswordHasher().Verify("Secreto123", entity.strPWD));
            Assert.Null(entity.str2FASecreto);
            Assert.False(entity.bln2FAHabilitado);
            Assert.NotNull(entity.dteFechaRegistro);
        }

        [Fact]
        public void FakeHasherRoundtripsAndRejectsWrongPassword()
        {
            var hasher = new FakeSegUsuarioPasswordHasher();
            var hash = hasher.Hash("Secreto123");

            Assert.True(hasher.Verify("Secreto123", hash));
            Assert.False(hasher.Verify("Otro12345", hash));
            Assert.Throws<ArgumentNullException>(() => hasher.Hash(null!));
            Assert.Throws<ArgumentNullException>(() => hasher.Verify(null!, hash));
            Assert.Throws<ArgumentNullException>(() => hasher.Verify("x", null!));
        }

        [Fact]
        public async Task NullDtosThrowArgumentNull()
        {
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateAsync(1, null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.DeleteAsync(1, null!));
        }

        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            var cache = new FakeCacheService();
            var hasher = new FakeSegUsuarioPasswordHasher();
            using var context = CreateContext();

            Assert.Throws<ArgumentNullException>(() => new SegUsuarioService(null!, cache, hasher));
            Assert.Throws<ArgumentNullException>(() => new SegUsuarioService(context, null!, hasher));
            Assert.Throws<ArgumentNullException>(() => new SegUsuarioService(context, cache, null!));
        }

        [Fact]
        public async Task ReadsDoNotTrackEntities()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            context.SegUsuarios.Add(new UsuarioModel
            {
                strNombre = "Track",
                strCorreoElectronico = "track@example.com",
                strPWD = "hash",
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var service = new SegUsuarioService(context, cache, new FakeSegUsuarioPasswordHasher());

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
            var service = new SegUsuarioService(context, cache, new FakeSegUsuarioPasswordHasher());
            var created = await service.CreateAsync(new SegUsuarioCreateDto
            {
                strNombre = "Key",
                strCorreoElectronico = "key@example.com",
                strPasswordPlano = "Secreto123",
            });
            await service.GetByIdAsync(created.id);
            await service.GetPagedAsync(1, 20);
            await service.SearchByNameAsync("Key", 1, 20);
            await service.AutocompleteAsync("Key", 10);

            Assert.Contains(cache.Keys, key => key == "cache:usuario:" + created.id);
            Assert.Contains(cache.Keys, key => key == "cache:usuario:page:1:1:20");
            Assert.Contains(cache.Keys, key => key == "cache:usuario:search:1:Key:1:20");
            Assert.Contains(cache.Keys, key => key == "cache:usuario:autocomplete:1:Key:10");
            Assert.Contains(cache.Keys, key => key == "cache:usuario:version");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
        }

        [Fact]
        public async Task GetByIdServesFromCacheAfterDelete()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, cache, new FakeSegUsuarioPasswordHasher());
            var created = await service.CreateAsync(new SegUsuarioCreateDto
            {
                strNombre = "Beto",
                strCorreoElectronico = "beto@example.com",
                strPasswordPlano = "Secreto123",
            });

            var first = await service.GetByIdAsync(created.id);
            context.SegUsuarios.Remove(await context.SegUsuarios.SingleAsync(e => e.id == created.id));
            await context.SaveChangesAsync();
            var second = await service.GetByIdAsync(created.id);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(first.strNombre, second.strNombre);
        }

        [Fact]
        public async Task GetByIdReturnsNullWhenMissing()
        {
            using var context = CreateContext();
            var service = new SegUsuarioService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());

            Assert.Null(await service.GetByIdAsync(999));
        }

        [Fact]
        public async Task GetPagedReturnsOrderedPagesAndCaches()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, cache, new FakeSegUsuarioPasswordHasher());
            context.SegUsuarios.AddRange(
                new UsuarioModel { id = 3, strNombre = "C3", strCorreoElectronico = "c3@example.com", strPWD = "h" },
                new UsuarioModel { id = 1, strNombre = "C1", strCorreoElectronico = "c1@example.com", strPWD = "h" },
                new UsuarioModel { id = 2, strNombre = "C2", strCorreoElectronico = "c2@example.com", strPWD = "h" });
            await context.SaveChangesAsync();

            var page1 = await service.GetPagedAsync(1, 2);
            var page2 = await service.GetPagedAsync(2, 2);

            Assert.Equal(3, page1.TotalCount);
            Assert.Equal(2, page1.Items.Count);
            Assert.Equal(1, page1.Items[0].id);
            Assert.Equal("C1", page1.Items[0].strNombre);
            Assert.Equal(2, page1.Items[1].id);
            Assert.Equal("C2", page1.Items[1].strNombre);
            Assert.Single(page2.Items);
            Assert.Equal(3, page2.Items[0].id);
            Assert.Equal("C3", page2.Items[0].strNombre);

            context.SegUsuarios.RemoveRange(context.SegUsuarios);
            await context.SaveChangesAsync();
            var cached = await service.GetPagedAsync(1, 2);
            Assert.Equal(3, cached.TotalCount);
            Assert.Equal("C1", cached.Items[0].strNombre);
        }

        [Fact]
        public async Task UpdateSucceedsAndInvalidatesById()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, cache, new FakeSegUsuarioPasswordHasher());
            var created = await service.CreateAsync(new SegUsuarioCreateDto
            {
                strNombre = "Dora",
                strCorreoElectronico = "dora@example.com",
                strPasswordPlano = "Secreto123",
            });
            var hashBefore = (await context.SegUsuarios.SingleAsync(e => e.id == created.id)).strPWD;

            var updated = await service.UpdateAsync(created.id, new SegUsuarioUpdateDto
            {
                id = created.id,
                strNombre = "Dora X",
                strCorreoElectronico = "dorax@example.com",
                RowVersion = created.RowVersion,
            });

            Assert.NotNull(updated);
            Assert.Equal("Dora X", updated.strNombre);
            Assert.Equal("dorax@example.com", updated.strCorreoElectronico);
            Assert.Contains(cache.Removed, key => key == "cache:usuario:" + created.id);
            var hashAfter = (await context.SegUsuarios.SingleAsync(e => e.id == created.id)).strPWD;
            Assert.Equal(hashBefore, hashAfter);
        }

        [Fact]
        public async Task UpdateReturnsNullWhenMissing()
        {
            using var context = CreateContext();
            var service = new SegUsuarioService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());

            var result = await service.UpdateAsync(999, new SegUsuarioUpdateDto
            {
                id = 999,
                strNombre = "Nadie",
                strCorreoElectronico = "nadie@example.com",
                RowVersion = new byte[] { 1 },
            });

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateWithStaleRowVersionThrowsConflict()
        {
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());
            var created = await service.CreateAsync(new SegUsuarioCreateDto
            {
                strNombre = "Eva",
                strCorreoElectronico = "eva@example.com",
                strPasswordPlano = "Secreto123",
            });

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.UpdateAsync(created.id, new SegUsuarioUpdateDto
            {
                id = created.id,
                strNombre = "Eva X",
                strCorreoElectronico = "evax@example.com",
                RowVersion = new byte[] { 9 },
            }));
        }

        [Fact]
        public async Task DeleteRemovesAndReturnsTrue()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, cache, new FakeSegUsuarioPasswordHasher());
            var created = await service.CreateAsync(new SegUsuarioCreateDto
            {
                strNombre = "Fin",
                strCorreoElectronico = "fin@example.com",
                strPasswordPlano = "Secreto123",
            });
            cache.Removed.Clear();

            var deleted = await service.DeleteAsync(created.id, new SegUsuarioDeleteDto { id = created.id, RowVersion = created.RowVersion });

            Assert.True(deleted);
            Assert.Null(await context.SegUsuarios.SingleOrDefaultAsync(e => e.id == created.id));
            Assert.Contains(cache.Removed, key => key == "cache:usuario:" + created.id);
        }

        [Fact]
        public async Task DeleteReturnsFalseWhenMissing()
        {
            using var context = CreateContext();
            var service = new SegUsuarioService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());

            Assert.False(await service.DeleteAsync(999, new SegUsuarioDeleteDto { id = 999, RowVersion = new byte[] { 1 } }));
        }

        [Fact]
        public async Task DeleteWithStaleRowVersionThrowsConflict()
        {
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());
            var created = await service.CreateAsync(new SegUsuarioCreateDto
            {
                strNombre = "Gus",
                strCorreoElectronico = "gus@example.com",
                strPasswordPlano = "Secreto123",
            });

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.DeleteAsync(created.id, new SegUsuarioDeleteDto { id = created.id, RowVersion = new byte[] { 9 } }));
        }

        [Fact]
        public async Task SearchByNameReturnsPagedOrderedAndCaches()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, cache, new FakeSegUsuarioPasswordHasher());
            context.SegUsuarios.AddRange(
                new UsuarioModel { strNombre = "Ana Paula", strCorreoElectronico = "a1@example.com", strPWD = "h" },
                new UsuarioModel { strNombre = "Ana Maria", strCorreoElectronico = "a2@example.com", strPWD = "h" },
                new UsuarioModel { strNombre = "Pedro", strCorreoElectronico = "p@example.com", strPWD = "h" });
            await context.SaveChangesAsync();

            var result = await service.SearchByNameAsync("Ana", 1, 20);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.True(result.Items[0].id < result.Items[1].id);
            Assert.All(result.Items, item => Assert.Contains("Ana", item.strNombre, StringComparison.Ordinal));

            var page2 = await service.SearchByNameAsync("Ana", 2, 2);
            Assert.Equal(2, page2.TotalCount);
            Assert.Empty(page2.Items);

            context.SegUsuarios.RemoveRange(context.SegUsuarios);
            await context.SaveChangesAsync();
            var cached = await service.SearchByNameAsync("Ana", 1, 20);
            Assert.Equal(2, cached.TotalCount);
        }

        [Fact]
        public async Task SearchByNameTrimsTexto()
        {
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());
            context.SegUsuarios.Add(new UsuarioModel { strNombre = "Ana", strCorreoElectronico = "a@example.com", strPWD = "h" });
            await context.SaveChangesAsync();

            var result = await service.SearchByNameAsync("  Ana  ", 1, 20);

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Ana", result.Items[0].strNombre);
        }

        [Fact]
        public async Task SearchByNameEmptyReturnsEmptyWithoutQueryingCache()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, cache, new FakeSegUsuarioPasswordHasher());

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
        public async Task SearchResultsNeverLeakSecrets()
        {
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());
            await service.CreateAsync(new SegUsuarioCreateDto
            {
                strNombre = "Secreta",
                strCorreoElectronico = "s@example.com",
                strPasswordPlano = "Secreto123",
            });

            var result = await service.SearchByNameAsync("Secreta", 1, 20);
            var json = JsonSerializer.Serialize(result);

            Assert.DoesNotContain("strPWD", json, StringComparison.Ordinal);
            Assert.DoesNotContain("str2FASecreto", json, StringComparison.Ordinal);
            Assert.DoesNotContain("Secreto123", json, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AutocompleteReturnsTopNOrderedWithMinimalShape()
        {
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());
            context.SegUsuarios.AddRange(
                new UsuarioModel { id = 3, strNombre = "Ana C", strCorreoElectronico = "c@example.com", strPWD = "h" },
                new UsuarioModel { id = 1, strNombre = "Ana A", strCorreoElectronico = "a@example.com", strPWD = "h" },
                new UsuarioModel { id = 2, strNombre = "Ana B", strCorreoElectronico = "b@example.com", strPWD = "h" });
            await context.SaveChangesAsync();

            var top2 = await service.AutocompleteAsync("Ana", 2);

            Assert.Equal(2, top2.Count);
            Assert.Equal(1, top2[0].id);
            Assert.Equal("Ana A", top2[0].strNombre);
            Assert.Equal(2, top2[1].id);

            var json = JsonSerializer.Serialize(top2);
            Assert.DoesNotContain("strPWD", json, StringComparison.Ordinal);
            Assert.DoesNotContain("strCorreo", json, StringComparison.Ordinal);
            Assert.DoesNotContain("str2FASecreto", json, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AutocompleteTrimsTexto()
        {
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());
            context.SegUsuarios.Add(new UsuarioModel { strNombre = "Luis", strCorreoElectronico = "l@example.com", strPWD = "h" });
            await context.SaveChangesAsync();

            var result = await service.AutocompleteAsync("  Luis  ", 10);

            Assert.Single(result);
            Assert.Equal("Luis", result[0].strNombre);
        }

        [Fact]
        public async Task AutocompleteNormalizesOutOfRangeToTen()
        {
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());
            for (var i = 0; i < 12; i++)
            {
                context.SegUsuarios.Add(new UsuarioModel { strNombre = $"Anexo{i}", strCorreoElectronico = $"a{i}@example.com", strPWD = "h" });
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
            var service = new SegUsuarioService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());
            for (var i = 0; i < 12; i++)
            {
                context.SegUsuarios.Add(new UsuarioModel { strNombre = $"Borde{i}", strCorreoElectronico = $"b{i}@example.com", strPWD = "h" });
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
            var service = new SegUsuarioService(context, cache, new FakeSegUsuarioPasswordHasher());

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
            var service = new SegUsuarioService(context, cache, new FakeSegUsuarioPasswordHasher());
            context.SegUsuarios.Add(new UsuarioModel { strNombre = "CacheMe", strCorreoElectronico = "c@example.com", strPWD = "h" });
            await context.SaveChangesAsync();

            var first = await service.AutocompleteAsync("CacheMe", 10);
            context.SegUsuarios.RemoveRange(context.SegUsuarios);
            await context.SaveChangesAsync();
            var second = await service.AutocompleteAsync("CacheMe", 10);

            Assert.Single(first);
            Assert.Single(second);
            Assert.Equal(first[0].strNombre, second[0].strNombre);
        }

        [Fact]
        public async Task DtoSerializationNeverLeaksSecrets()
        {
            await using var context = CreateContext();
            var service = new SegUsuarioService(context, new FakeCacheService(), new FakeSegUsuarioPasswordHasher());
            var created = await service.CreateAsync(new SegUsuarioCreateDto
            {
                strNombre = "NoLeak",
                strCorreoElectronico = "noleak@example.com",
                strPasswordPlano = "Secreto123",
            });

            var fetched = await service.GetByIdAsync(created.id);
            var paged = await service.GetPagedAsync(1, 20);
            var json = JsonSerializer.Serialize(new { fetched, paged });

            Assert.DoesNotContain("strPWD", json, StringComparison.Ordinal);
            Assert.DoesNotContain("str2FASecreto", json, StringComparison.Ordinal);
            Assert.DoesNotContain("Secreto123", json, StringComparison.Ordinal);
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
