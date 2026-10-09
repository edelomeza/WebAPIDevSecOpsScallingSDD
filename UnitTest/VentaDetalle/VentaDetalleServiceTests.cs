using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;
using ClienteModel = WebAPIDevSecOpsScallingSDD.Models.CliCliente;
using DetalleModel = WebAPIDevSecOpsScallingSDD.Models.VenVentaDetalle;
using EstadoModel = WebAPIDevSecOpsScallingSDD.Models.VenCatEstado;
using ProductoModel = WebAPIDevSecOpsScallingSDD.Models.ProProducto;
using UsuarioModel = WebAPIDevSecOpsScallingSDD.Models.SegUsuario;
using VentaModel = WebAPIDevSecOpsScallingSDD.Models.VenVenta;

namespace UnitTest.VentaDetalle
{
    public class VentaDetalleServiceTests
    {
        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            using var context = CreateContext();
            var cache = new FakeCacheService();

            Assert.Throws<ArgumentNullException>(() => new VentaDetalleService(null!, cache));
            Assert.Throws<ArgumentNullException>(() => new VentaDetalleService(context, null!));
        }

        [Fact]
        public async Task NullDtosThrowArgumentNull()
        {
            await using var context = CreateContext();
            var service = new VentaDetalleService(context, new FakeCacheService());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.AddDetalleAsync(1, null!, "1"));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.RemoveDetalleAsync(1, null!, "1"));
        }

        [Fact]
        public async Task AddPersistsDiscountsStockAndInvalidatesVentaCache()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaDetalleService(context, cache);

            var created = await service.AddDetalleAsync(1, new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 2 }, "1");

            Assert.NotNull(created);
            Assert.True(created!.id > 0);
            Assert.Equal(1, created.idProProducto);
            Assert.Equal(2, created.intPiezaVenta);
            Assert.Equal(20m, created.decTotalVenta);
            Assert.Equal(3, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
            Assert.Equal(2, await context.VenVentaDetalles.CountAsync());
            Assert.Equal(2, await context.VenVentaDetalles.CountAsync());
            Assert.Contains(cache.Keys, key => key == "cache:venta:version");
            Assert.Contains(cache.Keys, key => key == "cache:producto:version");
            Assert.Equal(1, await cache.GetAsync<int>("cache:", "venta:version"));
            Assert.Equal(1, await cache.GetAsync<int>("cache:", "producto:version"));
            Assert.Contains(cache.Removed, key => key == "cache:venta:1");
            Assert.Contains(cache.Removed, key => key == "cache:producto:1");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
        }

        [Fact]
        public async Task AddMissingVentaReturnsNull()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaDetalleService(context, new FakeCacheService());

            var result = await service.AddDetalleAsync(999, new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 1 }, "1");

            Assert.Null(result);
            Assert.Equal(5, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
        }

        [Fact]
        public async Task AddUnknownProductThrowsValidation()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaDetalleService(context, new FakeCacheService());

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.AddDetalleAsync(1, new VenVentaDetalleCreateDto { idProProducto = 999, intPiezaVenta = 1 }, "1"));
            Assert.Contains("999", ex.Message, StringComparison.Ordinal);
            Assert.Equal(1, await context.VenVentaDetalles.CountAsync());
        }

        [Fact]
        public async Task AddInsufficientStockThrowsConflictWithoutWrites()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 1, precio: 10m);
            var service = new VentaDetalleService(context, new FakeCacheService());

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.AddDetalleAsync(1, new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 2 }, "1"));
            Assert.Equal(1, await context.VenVentaDetalles.CountAsync());
            Assert.Equal(1, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
        }

        [Fact]
        public async Task AddExactStockSucceedsLeavingZero()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 2, precio: 10m);
            var service = new VentaDetalleService(context, new FakeCacheService());

            var created = await service.AddDetalleAsync(1, new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 2 }, "1");

            Assert.NotNull(created);
            Assert.Equal(0, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
        }

        [Fact]
        public async Task AddWithWrongOwnerThrowsUnauthorized()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaDetalleService(context, new FakeCacheService());

            var stranger = await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.AddDetalleAsync(1, new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 1 }, "2"));
            Assert.Contains("usuario autenticado", stranger.Message, StringComparison.Ordinal);
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.AddDetalleAsync(1, new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 1 }, null));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.AddDetalleAsync(1, new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 1 }, "   "));
            Assert.Equal(1, await context.VenVentaDetalles.CountAsync());
        }

        [Fact]
        public async Task GetByIdReturnsNullWhenMissingAndDtoWhenFound()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaDetalleService(context, cache);

            Assert.Null(await service.GetByIdAsync(999));

            var created = await service.AddDetalleAsync(1, new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 1 }, "1");
            var fetched = await service.GetByIdAsync(created!.id);

            Assert.NotNull(fetched);
            Assert.Equal(created.id, fetched!.id);
            Assert.Equal(10m, fetched.decTotalVenta);
        }

        [Fact]
        public async Task RemoveRestoresStockAndInvalidatesVentaCache()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaDetalleService(context, cache);
            var created = await service.AddDetalleAsync(1, new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 2 }, "1");
            Assert.Equal(3, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
            cache.Removed.Clear();

            var deleted = await service.RemoveDetalleAsync(created!.id, new VenVentaDetalleDeleteDto { id = created.id, RowVersion = created.RowVersion }, "1");

            Assert.True(deleted);
            Assert.Null(await context.VenVentaDetalles.SingleOrDefaultAsync(e => e.id == created.id));
            Assert.Equal(5, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
            Assert.Contains(cache.Removed, key => key == "cache:venta:1");
            Assert.Contains(cache.Removed, key => key == "cache:producto:1");
        }

        [Fact]
        public async Task RemoveMissingReturnsFalse()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaDetalleService(context, new FakeCacheService());

            Assert.False(await service.RemoveDetalleAsync(999, new VenVentaDetalleDeleteDto { id = 999, RowVersion = new byte[] { 1 } }, "1"));
        }

        [Fact]
        public async Task RemoveWithWrongOwnerThrowsUnauthorized()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaDetalleService(context, new FakeCacheService());
            var created = await service.AddDetalleAsync(1, new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 1 }, "1");

            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.RemoveDetalleAsync(created!.id, new VenVentaDetalleDeleteDto { id = created.id, RowVersion = created.RowVersion }, "2"));
            Assert.NotNull(await context.VenVentaDetalles.SingleOrDefaultAsync(e => e.id == created!.id));
        }

        [Fact]
        public async Task RemoveWithStaleRowVersionThrowsConflict()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaDetalleService(context, new FakeCacheService());
            var created = await service.AddDetalleAsync(1, new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 1 }, "1");

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.RemoveDetalleAsync(created!.id, new VenVentaDetalleDeleteDto { id = created.id, RowVersion = new byte[] { 9 } }, "1"));
        }

        [Fact]
        public async Task ReadsDoNotTrackEntities()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            context.ChangeTracker.Clear();
            var service = new VentaDetalleService(context, cache);

            await service.GetByIdAsync(1);
            await service.AutocompleteProductoAsync("Tornillo", 10);

            Assert.Empty(context.ChangeTracker.Entries());
        }

        [Fact]
        public async Task CacheKeysFollowNamingConvention()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaDetalleService(context, cache);

            await service.AddDetalleAsync(1, new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 1 }, "1");
            await service.AutocompleteProductoAsync("Tornillo", 10);

            Assert.Contains(cache.Keys, key => key == "cache:venta:version");
            Assert.Contains(cache.Keys, key => key == "cache:producto:version");
            Assert.Contains(cache.Keys, key => key == "cache:producto:autocomplete:1:Tornillo:10");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
        }

        [Fact]
        public async Task AutocompleteReturnsTopNOrderedWithMinimalShape()
        {
            await using var context = CreateContext();
            var service = new VentaDetalleService(context, new FakeCacheService());
            context.ProProductos.AddRange(
                new ProductoModel { id = 3, strNombreProducto = "Tornillo C", intNumeroExistencia = 5, decPrecio = 1m },
                new ProductoModel { id = 1, strNombreProducto = "Tornillo A", intNumeroExistencia = 5, decPrecio = 2m },
                new ProductoModel { id = 2, strNombreProducto = "Tornillo B", intNumeroExistencia = 5, decPrecio = 3m });
            await context.SaveChangesAsync();

            var top2 = await service.AutocompleteProductoAsync("Tornillo", 2);

            Assert.Equal(2, top2.Count);
            Assert.Equal(1, top2[0].id);
            Assert.Equal("Tornillo A", top2[0].strNombreProducto);
            Assert.Equal(2, top2[1].id);

            var json = JsonSerializer.Serialize(top2);
            Assert.DoesNotContain("decPrecio", json, StringComparison.Ordinal);
            Assert.DoesNotContain("intNumeroExistencia", json, StringComparison.Ordinal);
            Assert.DoesNotContain("RowVersion", json, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AutocompleteTrimsTexto()
        {
            await using var context = CreateContext();
            var service = new VentaDetalleService(context, new FakeCacheService());
            context.ProProductos.Add(new ProductoModel { strNombreProducto = "Martillo", intNumeroExistencia = 5, decPrecio = 1m });
            await context.SaveChangesAsync();

            var result = await service.AutocompleteProductoAsync("  Martillo  ", 10);

            Assert.Single(result);
            Assert.Equal("Martillo", result[0].strNombreProducto);
        }

        [Fact]
        public async Task AutocompleteNormalizesOutOfRangeToTen()
        {
            await using var context = CreateContext();
            var service = new VentaDetalleService(context, new FakeCacheService());
            for (var i = 0; i < 12; i++)
            {
                context.ProProductos.Add(new ProductoModel { strNombreProducto = $"Tuerca{i}", intNumeroExistencia = 5, decPrecio = 1m });
            }

            await context.SaveChangesAsync();

            var zero = await service.AutocompleteProductoAsync("Tuerca", 0);
            var over = await service.AutocompleteProductoAsync("Tuerca", 51);
            var negative = await service.AutocompleteProductoAsync("Tuerca", -5);
            var one = await service.AutocompleteProductoAsync("Tuerca", 1);

            Assert.Equal(10, zero.Count);
            Assert.Equal(10, over.Count);
            Assert.Equal(10, negative.Count);
            Assert.Single(one);
        }

        [Fact]
        public async Task AutocompleteBoundaryFiftyIsNotNormalized()
        {
            await using var context = CreateContext();
            var service = new VentaDetalleService(context, new FakeCacheService());
            for (var i = 0; i < 12; i++)
            {
                context.ProProductos.Add(new ProductoModel { strNombreProducto = $"Perno{i}", intNumeroExistencia = 5, decPrecio = 1m });
            }

            await context.SaveChangesAsync();

            var fifty = await service.AutocompleteProductoAsync("Perno", 50);

            Assert.Equal(12, fifty.Count);
        }

        [Fact]
        public async Task AutocompleteEmptyReturnsEmptyWithoutQueryingCache()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new VentaDetalleService(context, cache);

            var blank = await service.AutocompleteProductoAsync("   ", 10);
            var nulled = await service.AutocompleteProductoAsync(null!, 10);

            Assert.Empty(blank);
            Assert.Empty(nulled);
            Assert.Empty(cache.Keys);
        }

        [Fact]
        public async Task AutocompleteCachesResults()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var service = new VentaDetalleService(context, cache);
            context.ProProductos.Add(new ProductoModel { strNombreProducto = "CacheMe", intNumeroExistencia = 5, decPrecio = 1m });
            await context.SaveChangesAsync();

            var first = await service.AutocompleteProductoAsync("CacheMe", 10);
            context.ProProductos.RemoveRange(context.ProProductos);
            await context.SaveChangesAsync();
            var second = await service.AutocompleteProductoAsync("CacheMe", 10);

            Assert.Single(first);
            Assert.Single(second);
            Assert.Equal(first[0].strNombreProducto, second[0].strNombreProducto);
        }

        private static void SeedBase(AppDbContext context, int existencia, decimal precio)
        {
            context.CliClientes.Add(new ClienteModel { id = 1, strNombreCliente = "Ana", strCorreoElectronico = "a@test.local", strNumeroTelefono = "5550000001" });
            context.SegUsuarios.Add(new UsuarioModel { id = 1, strNombre = "Ana", strPWD = "hash", strCorreoElectronico = "a@test.local" });
            context.VenCatEstados.Add(new EstadoModel { id = 1, strValor = "Vigente" });
            context.ProProductos.Add(new ProductoModel { id = 1, strNombreProducto = "Tornillo", intNumeroExistencia = existencia, decPrecio = precio });
            context.VenVentas.Add(new VentaModel { id = 1, idCliCliente = 1, idSegUsuario = 1, idVenCatEstado = 1, strClaveVenta = "V-00000001" });
            context.VenVentaDetalles.Add(new DetalleModel { id = 1, idVenVenta = 1, idProProducto = 1, intPiezaVenta = 1, decTotalVenta = precio });
            context.SaveChanges();
            context.ChangeTracker.Clear();
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
