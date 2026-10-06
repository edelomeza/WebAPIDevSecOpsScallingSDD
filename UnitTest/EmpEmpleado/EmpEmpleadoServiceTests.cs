using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;
using EmpleadoModel = WebAPIDevSecOpsScallingSDD.Models.EmpEmpleado;
using TipoModel = WebAPIDevSecOpsScallingSDD.Models.EmpCatTipoEmpleado;

namespace UnitTest.EmpEmpleado
{
    public class EmpEmpleadoServiceTests
    {
        private const string ValidCurp = "SAAA260101HDFXXX01";

        [Fact]
        public async Task CreatePersistsAndRotatesCacheVersion()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, cache);

            var created = await service.CreateAsync(new EmpEmpleadoCreateDto
            {
                strNombre = "Ana",
                strCURP = ValidCurp,
            });

            Assert.True(created.id > 0);
            Assert.Equal("Ana", (await service.GetByIdAsync(created.id))?.strNombre);
            Assert.Equal(1, await cache.GetAsync<int>("cache:", "empleado:version"));
            Assert.Empty(cache.Removed);
        }

        [Fact]
        public async Task NullDtosThrowArgumentNull()
        {
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateAsync(1, null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.DeleteAsync(1, null!));
        }

        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            var cache = new FakeCacheService();
            using var context = CreateContext();

            Assert.Throws<ArgumentNullException>(() => new EmpEmpleadoService(null!, cache));
            Assert.Throws<ArgumentNullException>(() => new EmpEmpleadoService(context, null!));
        }

        [Fact]
        public async Task ReadsDoNotTrackEntities()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            context.EmpEmpleados.Add(new EmpleadoModel { strNombre = "Track" });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var service = new EmpEmpleadoService(context, cache);

            await service.GetByIdAsync(1);
            await service.GetPagedAsync(1, 20);
            await service.SearchAsync("Track", null, 1, 20);

            Assert.Empty(context.ChangeTracker.Entries());
        }

        [Fact]
        public async Task CacheKeysFollowNamingConvention()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, cache);
            var created = await service.CreateAsync(new EmpEmpleadoCreateDto { strNombre = "Key" });
            await service.GetByIdAsync(created.id);
            await service.GetPagedAsync(1, 20);
            await service.SearchAsync("Key", null, 1, 20);

            Assert.Contains(cache.Keys, key => key == "cache:empleado:" + created.id);
            Assert.Contains(cache.Keys, key => key == "cache:empleado:page:1:1:20");
            Assert.Contains(cache.Keys, key => key == "cache:empleado:search:1:Key:null:1:20");
            Assert.Contains(cache.Keys, key => key == "cache:empleado:version");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
        }

        [Fact]
        public async Task GetByIdServesFromCacheAfterDelete()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, cache);
            var created = await service.CreateAsync(new EmpEmpleadoCreateDto { strNombre = "Beto" });

            var first = await service.GetByIdAsync(created.id);
            context.EmpEmpleados.Remove(await context.EmpEmpleados.SingleAsync(e => e.id == created.id));
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
            var service = new EmpEmpleadoService(context, new FakeCacheService());

            Assert.Null(await service.GetByIdAsync(999));
        }

        [Fact]
        public async Task GetPagedReturnsOrderedPagesAndCaches()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, cache);
            context.EmpEmpleados.AddRange(
                new EmpleadoModel { id = 3, strNombre = "C3" },
                new EmpleadoModel { id = 1, strNombre = "C1" },
                new EmpleadoModel { id = 2, strNombre = "C2" });
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

            context.EmpEmpleados.RemoveRange(context.EmpEmpleados);
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
            var service = new EmpEmpleadoService(context, cache);
            var created = await service.CreateAsync(new EmpEmpleadoCreateDto { strNombre = "Dora" });

            var updated = await service.UpdateAsync(created.id, new EmpEmpleadoUpdateDto
            {
                id = created.id,
                strNombre = "Dora X",
                RowVersion = created.RowVersion,
            });

            Assert.NotNull(updated);
            Assert.Equal("Dora X", updated.strNombre);
            Assert.Contains(cache.Removed, key => key == "cache:empleado:" + created.id);
        }

        [Fact]
        public async Task UpdateReturnsNullWhenMissing()
        {
            using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());

            var result = await service.UpdateAsync(999, new EmpEmpleadoUpdateDto
            {
                id = 999,
                strNombre = "Nadie",
                RowVersion = new byte[] { 1 },
            });

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateWithStaleRowVersionThrowsConflict()
        {
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());
            var created = await service.CreateAsync(new EmpEmpleadoCreateDto { strNombre = "Eva" });

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.UpdateAsync(created.id, new EmpEmpleadoUpdateDto
            {
                id = created.id,
                strNombre = "Eva X",
                RowVersion = new byte[] { 9 },
            }));
        }

        [Fact]
        public async Task DeleteRemovesAndReturnsTrue()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, cache);
            var created = await service.CreateAsync(new EmpEmpleadoCreateDto { strNombre = "Fin" });

            var deleted = await service.DeleteAsync(created.id, new EmpEmpleadoDeleteDto { id = created.id, RowVersion = created.RowVersion });

            Assert.True(deleted);
            Assert.Null(await service.GetByIdAsync(created.id));
            Assert.Contains(cache.Removed, key => key == "cache:empleado:" + created.id);
        }

        [Fact]
        public async Task DeleteReturnsFalseWhenMissing()
        {
            using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());

            Assert.False(await service.DeleteAsync(999, new EmpEmpleadoDeleteDto { id = 999, RowVersion = new byte[] { 1 } }));
        }

        [Fact]
        public async Task DeleteWithStaleRowVersionThrowsConflict()
        {
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());
            var created = await service.CreateAsync(new EmpEmpleadoCreateDto { strNombre = "Gus" });

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.DeleteAsync(created.id, new EmpEmpleadoDeleteDto { id = created.id, RowVersion = new byte[] { 9 } }));
        }

        [Fact]
        public async Task CreateWithNullTipoEmpleadoSucceeds()
        {
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());

            var created = await service.CreateAsync(new EmpEmpleadoCreateDto
            {
                strNombre = "SinTipo",
                idEmpCatTipoEmpleado = null,
            });

            Assert.True(created.id > 0);
            Assert.Null(created.idEmpCatTipoEmpleado);
        }

        [Fact]
        public async Task CreateWithValidTipoEmpleadoSucceeds()
        {
            await using var context = CreateContext();
            context.EmpCatTipoEmpleados.Add(new TipoModel { id = 1, strValor = "Cajero", strDescripcion = "Caja" });
            await context.SaveChangesAsync();
            var service = new EmpEmpleadoService(context, new FakeCacheService());

            var created = await service.CreateAsync(new EmpEmpleadoCreateDto
            {
                strNombre = "ConTipo",
                idEmpCatTipoEmpleado = 1,
            });

            Assert.Equal(1, created.idEmpCatTipoEmpleado);
        }

        [Fact]
        public async Task CreateWithUnknownTipoEmpleadoThrowsValidation()
        {
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new EmpEmpleadoCreateDto
            {
                strNombre = "MalTipo",
                idEmpCatTipoEmpleado = 999,
            }));
            Assert.Contains("no existe", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task UpdateWithUnknownTipoEmpleadoThrowsValidation()
        {
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());
            var created = await service.CreateAsync(new EmpEmpleadoCreateDto { strNombre = "Eva" });

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(created.id, new EmpEmpleadoUpdateDto
            {
                id = created.id,
                strNombre = "Eva X",
                idEmpCatTipoEmpleado = 999,
                RowVersion = created.RowVersion,
            }));
            Assert.Contains("no existe", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SearchFiltersByNombre()
        {
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());
            context.EmpEmpleados.AddRange(
                new EmpleadoModel { strNombre = "Ana Paula" },
                new EmpleadoModel { strNombre = "Pedro" });
            await context.SaveChangesAsync();

            var result = await service.SearchAsync("Ana", null, 1, 20);

            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal("Ana Paula", result.Items[0].strNombre);
        }

        [Fact]
        public async Task SearchFiltersByApellidosWithNullGuards()
        {
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());
            context.EmpEmpleados.AddRange(
                new EmpleadoModel { strNombre = "Juan", strAPaterno = "Garcia", strAMaterno = null },
                new EmpleadoModel { strNombre = "Luis", strAPaterno = "Perez", strAMaterno = "Lopez" });
            await context.SaveChangesAsync();

            var paterno = await service.SearchAsync("Garcia", null, 1, 20);
            var materno = await service.SearchAsync("Lopez", null, 1, 20);

            Assert.Equal(1, paterno.TotalCount);
            Assert.Equal("Juan", paterno.Items[0].strNombre);
            Assert.Equal(1, materno.TotalCount);
            Assert.Equal("Luis", materno.Items[0].strNombre);
        }

        [Fact]
        public async Task SearchCombinesTextoAndTipo()
        {
            await using var context = CreateContext();
            context.EmpCatTipoEmpleados.AddRange(
                new TipoModel { id = 1, strValor = "Cajero", strDescripcion = "Caja" },
                new TipoModel { id = 2, strValor = "Gerente", strDescripcion = "Gestion" });
            await context.SaveChangesAsync();
            var service = new EmpEmpleadoService(context, new FakeCacheService());
            context.EmpEmpleados.AddRange(
                new EmpleadoModel { strNombre = "Ana", idEmpCatTipoEmpleado = 1 },
                new EmpleadoModel { strNombre = "Ana", idEmpCatTipoEmpleado = 2 });
            await context.SaveChangesAsync();

            var combined = await service.SearchAsync("Ana", 1, 1, 20);
            var tipoOnly = await service.SearchAsync(null, 2, 1, 20);

            Assert.Equal(1, combined.TotalCount);
            Assert.Equal(1, combined.Items[0].idEmpCatTipoEmpleado);
            Assert.Equal(1, tipoOnly.TotalCount);
            Assert.Equal(2, tipoOnly.Items[0].idEmpCatTipoEmpleado);
        }

        [Fact]
        public async Task SearchWithoutFiltersReturnsAllPagedAndCaches()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, cache);
            context.EmpEmpleados.AddRange(
                new EmpleadoModel { id = 2, strNombre = "B2" },
                new EmpleadoModel { id = 1, strNombre = "B1" });
            await context.SaveChangesAsync();

            var result = await service.SearchAsync(null, null, 1, 20);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.Items[0].id);

            context.EmpEmpleados.RemoveRange(context.EmpEmpleados);
            await context.SaveChangesAsync();
            var cached = await service.SearchAsync(null, null, 1, 20);
            Assert.Equal(2, cached.TotalCount);
        }

        [Fact]
        public async Task SearchPaginatesSecondPage()
        {
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());
            context.EmpEmpleados.AddRange(
                new EmpleadoModel { strNombre = "Ana Uno" },
                new EmpleadoModel { strNombre = "Ana Dos" });
            await context.SaveChangesAsync();

            var page2 = await service.SearchAsync("Ana", null, 2, 2);

            Assert.Equal(2, page2.TotalCount);
            Assert.Empty(page2.Items);
        }

        [Fact]
        public async Task SearchTrimsTexto()
        {
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());
            context.EmpEmpleados.Add(new EmpleadoModel { strNombre = "Ana" });
            await context.SaveChangesAsync();

            var result = await service.SearchAsync("  Ana  ", null, 1, 20);

            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task SearchBlankTextoBehavesAsNoFilter()
        {
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());
            context.EmpEmpleados.AddRange(
                new EmpleadoModel { strNombre = "Ana" },
                new EmpleadoModel { strNombre = "Pedro" });
            await context.SaveChangesAsync();

            var result = await service.SearchAsync("   ", null, 1, 20);

            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task SearchUnknownTipoReturnsEmpty()
        {
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, new FakeCacheService());
            context.EmpEmpleados.Add(new EmpleadoModel { strNombre = "Ana", idEmpCatTipoEmpleado = 1 });
            await context.SaveChangesAsync();

            var result = await service.SearchAsync(null, 999, 1, 20);

            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task SearchCachesByCanonicalKey()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new EmpEmpleadoService(context, cache);
            context.EmpEmpleados.Add(new EmpleadoModel { strNombre = "CacheMe" });
            await context.SaveChangesAsync();

            await service.SearchAsync("CacheMe", 1, 1, 20);

            Assert.Contains(cache.Keys, key => key == "cache:empleado:search:0:CacheMe:1:1:20");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
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
