using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;
using ClienteModel = WebAPIDevSecOpsScallingSDD.Models.CliCliente;
using EstadoModel = WebAPIDevSecOpsScallingSDD.Models.VenCatEstado;
using ProductoModel = WebAPIDevSecOpsScallingSDD.Models.ProProducto;
using UsuarioModel = WebAPIDevSecOpsScallingSDD.Models.SegUsuario;

namespace UnitTest.Venta
{
    public class VentaServiceTests
    {
        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            using var context = CreateContext();
            var cache = new FakeCacheService();

            Assert.Throws<ArgumentNullException>(() => new VentaService(null!, cache));
            Assert.Throws<ArgumentNullException>(() => new VentaService(context, null!));
        }

        [Fact]
        public async Task NullDtoThrowsArgumentNull()
        {
            await using var context = CreateContext();
            var service = new VentaService(context, new FakeCacheService());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
        }

        [Fact]
        public async Task CreateDiscountsStockComputesTotalAndCaches()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaService(context, cache);

            var created = await service.CreateAsync(new VenVentaCreateDto
            {
                idCliCliente = 1,
                idSegUsuario = 1,
                idVenCatEstado = 1,
                strClaveVenta = "V-00000001",
                Detalles = new[] { new VenVentaDetalleCreateItemDto { idProProducto = 1, intPiezaVenta = 2 } },
            });

            Assert.Equal("V-00000001", created.strClaveVenta);
            Assert.NotNull(created.dteFechaHoraCompra);
            var detalle = Assert.Single(created.Detalles);
            Assert.Equal(1, detalle.idProProducto);
            Assert.Equal(2, detalle.intPiezaVenta);
            Assert.Equal(20m, detalle.decTotalVenta);
            Assert.Equal(3, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
            Assert.Equal(1, await context.VenVentas.CountAsync());
            Assert.Equal(1, await context.VenVentaDetalles.CountAsync());
            Assert.Contains(cache.Keys, key => key == "cache:venta:version");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
        }

        [Fact]
        public async Task UnknownClienteUsuarioEstadoThrowValidation()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaService(context, new FakeCacheService());

            await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(WithCliente(999)));
            await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(WithUsuario(999)));
            await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(WithEstado(999)));
            Assert.Equal(0, await context.VenVentas.CountAsync());
        }

        [Fact]
        public async Task UnknownProductThrowsValidation()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaService(context, new FakeCacheService());

            var dto = ValidDto();
            dto.Detalles = new[] { new VenVentaDetalleCreateItemDto { idProProducto = 999, intPiezaVenta = 1 } };

            await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto));
            Assert.Equal(0, await context.VenVentas.CountAsync());
        }

        [Fact]
        public async Task EmptyDetallesThrowsValidation()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaService(context, new FakeCacheService());

            var dto = ValidDto();
            dto.Detalles = Array.Empty<VenVentaDetalleCreateItemDto>();

            await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task InsufficientStockThrowsConflictWithoutWrites()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 1, precio: 10m);
            var service = new VentaService(context, new FakeCacheService());

            var dto = ValidDto();
            dto.Detalles = new[] { new VenVentaDetalleCreateItemDto { idProProducto = 1, intPiezaVenta = 2 } };

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.CreateAsync(dto));
            Assert.Equal(0, await context.VenVentas.CountAsync());
            Assert.Equal(1, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
        }

        [Fact]
        public async Task GetByIdReturnsNullWhenMissingAndCachesWhenFound()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentaService(context, cache);

            Assert.Null(await service.GetByIdAsync(999));

            var created = await service.CreateAsync(ValidDto());
            var first = await service.GetByIdAsync(created.id);
            var second = await service.GetByIdAsync(created.id);

            Assert.NotNull(first);
            Assert.Equal(created.id, first!.id);
            Assert.Equal(first.strClaveVenta, second!.strClaveVenta);
            Assert.Contains(cache.Keys, key => key == $"cache:venta:{created.id}");
        }

        private static VenVentaCreateDto ValidDto()
        {
            return new VenVentaCreateDto
            {
                idCliCliente = 1,
                idSegUsuario = 1,
                idVenCatEstado = 1,
                strClaveVenta = "V-00000001",
                Detalles = new[] { new VenVentaDetalleCreateItemDto { idProProducto = 1, intPiezaVenta = 1 } },
            };
        }

        private static VenVentaCreateDto WithCliente(int id)
        {
            var dto = ValidDto();
            dto.idCliCliente = id;
            return dto;
        }

        private static VenVentaCreateDto WithUsuario(int id)
        {
            var dto = ValidDto();
            dto.idSegUsuario = id;
            return dto;
        }

        private static VenVentaCreateDto WithEstado(int id)
        {
            var dto = ValidDto();
            dto.idVenCatEstado = id;
            return dto;
        }

        private static void SeedBase(AppDbContext context, int existencia, decimal precio)
        {
            context.CliClientes.Add(new ClienteModel { id = 1, strNombreCliente = "Ana", strCorreoElectronico = "a@test.local", strNumeroTelefono = "5550000001" });
            context.SegUsuarios.Add(new UsuarioModel { id = 1, strNombre = "Ana", strPWD = "hash", strCorreoElectronico = "a@test.local" });
            context.VenCatEstados.Add(new EstadoModel { id = 1, strValor = "Vigente" });
            context.ProProductos.Add(new ProductoModel { id = 1, strNombreProducto = "Tornillo", intNumeroExistencia = existencia, decPrecio = precio });
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
