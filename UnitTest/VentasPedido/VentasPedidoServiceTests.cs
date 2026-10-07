using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Events;
using WebAPIDevSecOpsScallingSDD.Services;
using WebAPIDevSecOpsScallingSDD.Validators;
using ClienteModel = WebAPIDevSecOpsScallingSDD.Models.CliCliente;
using PedidoModel = WebAPIDevSecOpsScallingSDD.Models.VenPedido;
using ProductoModel = WebAPIDevSecOpsScallingSDD.Models.ProProducto;

namespace UnitTest.VentasPedido
{
    public class VentasPedidoServiceTests
    {
        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            using var context = CreateContext();
            var cache = new FakeCacheService();
            var publisher = new FakePedidoEventPublisher();

            Assert.Throws<ArgumentNullException>(() => new VentasPedidoService(null!, cache, publisher));
            Assert.Throws<ArgumentNullException>(() => new VentasPedidoService(context, null!, publisher));
            Assert.Throws<ArgumentNullException>(() => new VentasPedidoService(context, cache, null!));
            Assert.Throws<ArgumentNullException>(() => new StockValidatorConsumer(null!));
        }

        [Fact]
        public async Task NullDtoThrowsArgumentNull()
        {
            await using var context = CreateContext();
            var service = new VentasPedidoService(context, new FakeCacheService(), new FakePedidoEventPublisher());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
        }

        [Fact]
        public async Task NullEventThrowsArgumentNull()
        {
            var publisher = new FakePedidoEventPublisher();

            await Assert.ThrowsAsync<ArgumentNullException>(() => publisher.PublishAsync(null!));
        }

        [Fact]
        public async Task EmptyDetallesThrowsValidationWithoutPublish()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var publisher = new FakePedidoEventPublisher();
            var service = new VentasPedidoService(context, new FakeCacheService(), publisher);

            var dto = ValidDto();
            dto.Detalles = Array.Empty<PedidoDetalleCreateDto>();

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto));
            Assert.Equal("El pedido requiere al menos un detalle.", ex.Message);
            Assert.Empty(publisher.Published);
            Assert.Equal(0, await context.VenPedidos.CountAsync());
        }

        [Fact]
        public async Task UnknownClienteThrowsValidationWithoutPublish()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var publisher = new FakePedidoEventPublisher();
            var service = new VentasPedidoService(context, new FakeCacheService(), publisher);

            var dto = ValidDto();
            dto.idCliCliente = 999;

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto));
            Assert.Equal("Cliente '999' no existe.", ex.Message);
            Assert.Empty(publisher.Published);
            Assert.Equal(0, await context.VenPedidos.CountAsync());
        }

        [Fact]
        public async Task UnknownProductThrowsValidationWithoutPublish()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var publisher = new FakePedidoEventPublisher();
            var service = new VentasPedidoService(context, new FakeCacheService(), publisher);

            var dto = ValidDto();
            dto.Detalles = new[] { new PedidoDetalleCreateDto { idProProducto = 999, intCantidad = 1 } };

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto));
            Assert.Equal("Producto '999' no existe.", ex.Message);
            Assert.Empty(publisher.Published);
            Assert.Equal(0, await context.VenPedidos.CountAsync());
        }

        [Fact]
        public async Task CreateComputesTotalPublishesEventAndCaches()
        {
            var cache = new FakeCacheService();
            var publisher = new FakePedidoEventPublisher();
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentasPedidoService(context, cache, publisher);

            var created = await service.CreateAsync(ValidDto());

            Assert.NotEqual(Guid.Empty, created.id);
            Assert.Equal(1, created.idCliCliente);
            Assert.Equal(20m, created.decTotal);
            Assert.Equal("Creado", created.strEstadoSaga);
            var detalle = Assert.Single(created.Detalles);
            Assert.Equal(1, detalle.idProProducto);
            Assert.Equal(2, detalle.intCantidad);
            Assert.Equal(10m, detalle.decPrecioUnitario);

            var published = Assert.Single(publisher.Published);
            Assert.Equal(created.id, published.PedidoId);
            Assert.Equal(1, published.ClienteId);
            Assert.Equal(20m, published.Total);

            Assert.Equal(1, await cache.GetAsync<int>("cache:", "pedido:version"));
            Assert.Contains(cache.Keys, key => key == "cache:pedido:version");
            var fetched = await service.GetByIdAsync(created.id);
            Assert.NotNull(fetched);
            Assert.Equal(created.id, fetched!.id);
            Assert.Contains(cache.Keys, key => key == $"cache:pedido:{created.id}");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
        }

        [Fact]
        public async Task CreateWithTwoDetallesPreservesOrderAndTotal()
        {
            var cache = new FakeCacheService();
            var publisher = new FakePedidoEventPublisher();
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            context.ProProductos.Add(new ProductoModel { id = 2, strNombreProducto = "Tuerca", intNumeroExistencia = 5, decPrecio = 5m });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var service = new VentasPedidoService(context, cache, publisher);

            var created = await service.CreateAsync(new PedidoCreateDto
            {
                idCliCliente = 1,
                Detalles = new[]
                {
                    new PedidoDetalleCreateDto { idProProducto = 1, intCantidad = 1 },
                    new PedidoDetalleCreateDto { idProProducto = 2, intCantidad = 3 },
                },
            });

            Assert.Equal(25m, created.decTotal);
            Assert.Equal(2, created.Detalles.Count);
            Assert.Equal(1, created.Detalles[0].idProProducto);
            Assert.Equal(2, created.Detalles[1].idProProducto);
            Assert.Equal(10m, created.Detalles[0].decPrecioUnitario);
            Assert.Equal(5m, created.Detalles[1].decPrecioUnitario);
        }

        [Fact]
        public async Task CreateDoesNotDiscountStock()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentasPedidoService(context, new FakeCacheService(), new FakePedidoEventPublisher());

            await service.CreateAsync(ValidDto());

            Assert.Equal(5, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
        }

        [Fact]
        public async Task GetByIdReturnsNullWhenMissingAndCachesWhenFound()
        {
            var cache = new FakeCacheService();
            var publisher = new FakePedidoEventPublisher();
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentasPedidoService(context, cache, publisher);

            Assert.Null(await service.GetByIdAsync(Guid.NewGuid()));

            context.ProProductos.Add(new ProductoModel { id = 2, strNombreProducto = "Tuerca", intNumeroExistencia = 5, decPrecio = 5m });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var created = await service.CreateAsync(new PedidoCreateDto
            {
                idCliCliente = 1,
                Detalles = new[]
                {
                    new PedidoDetalleCreateDto { idProProducto = 1, intCantidad = 1 },
                    new PedidoDetalleCreateDto { idProProducto = 2, intCantidad = 1 },
                },
            });
            var first = await service.GetByIdAsync(created.id);
            var second = await service.GetByIdAsync(created.id);

            Assert.NotNull(first);
            Assert.Equal(created.id, first!.id);
            Assert.Equal("Creado", first.strEstadoSaga);
            Assert.Equal(2, first.Detalles.Count);
            Assert.Equal(1, first.Detalles[0].idProProducto);
            Assert.Equal(2, first.Detalles[1].idProProducto);
            Assert.Equal(first.decTotal, second!.decTotal);
            Assert.Equal(first.Detalles.Count, second.Detalles.Count);
            Assert.Contains(cache.Keys, key => key == $"cache:pedido:{created.id}");
        }

        [Fact]
        public async Task GetByIdServesStaleCacheAfterDelete()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentasPedidoService(context, cache, new FakePedidoEventPublisher());

            var created = await service.CreateAsync(ValidDto());
            var first = await service.GetByIdAsync(created.id);
            Assert.NotNull(first);

            context.VenPedidoDetalles.RemoveRange(context.VenPedidoDetalles);
            context.VenPedidos.RemoveRange(context.VenPedidos);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            Assert.Equal(0, await context.VenPedidos.CountAsync());

            var stale = await service.GetByIdAsync(created.id);
            Assert.NotNull(stale);
            Assert.Equal(created.id, stale!.id);
            Assert.Equal(20m, stale.decTotal);
        }

        [Fact]
        public async Task StockValidatorDetectsAvailability()
        {
            await using var context = CreateContext();
            SeedBase(context, existencia: 5, precio: 10m);
            var service = new VentasPedidoService(context, new FakeCacheService(), new FakePedidoEventPublisher());
            var consumer = new StockValidatorConsumer(context);

            Assert.False(await consumer.HasStockAsync(Guid.NewGuid()));

            var created = await service.CreateAsync(ValidDto());
            Assert.True(await consumer.HasStockAsync(created.id));

            var producto = await context.ProProductos.SingleAsync();
            producto.intNumeroExistencia = 1;
            await context.SaveChangesAsync();
            Assert.False(await consumer.HasStockAsync(created.id));
        }

        [Fact]
        public void CreateValidatorRejectsInvalidIds()
        {
            var validator = new PedidoCreateValidator();

            Assert.False(validator.Validate(new PedidoCreateDto { idCliCliente = 0, Detalles = new[] { new PedidoDetalleCreateDto { idProProducto = 1, intCantidad = 1 } } }).IsValid);
            Assert.False(validator.Validate(new PedidoCreateDto { idCliCliente = 1, Detalles = Array.Empty<PedidoDetalleCreateDto>() }).IsValid);
            Assert.False(validator.Validate(new PedidoCreateDto { idCliCliente = 1, Detalles = new[] { new PedidoDetalleCreateDto { idProProducto = 0, intCantidad = 1 } } }).IsValid);
            Assert.False(validator.Validate(new PedidoCreateDto { idCliCliente = 1, Detalles = new[] { new PedidoDetalleCreateDto { idProProducto = 1, intCantidad = 0 } } }).IsValid);
            Assert.True(validator.Validate(ValidDto()).IsValid);
        }

        private static PedidoCreateDto ValidDto()
        {
            return new PedidoCreateDto
            {
                idCliCliente = 1,
                Detalles = new[] { new PedidoDetalleCreateDto { idProProducto = 1, intCantidad = 2 } },
            };
        }

        private static void SeedBase(AppDbContext context, int existencia, decimal precio)
        {
            context.CliClientes.Add(new ClienteModel { id = 1, strNombreCliente = "Ana", strCorreoElectronico = "a@test.local", strNumeroTelefono = "5550000001" });
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
