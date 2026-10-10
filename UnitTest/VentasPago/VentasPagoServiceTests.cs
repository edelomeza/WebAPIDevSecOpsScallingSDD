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
using PagoModel = WebAPIDevSecOpsScallingSDD.Models.VenPedidoPago;
using PedidoModel = WebAPIDevSecOpsScallingSDD.Models.VenPedido;

namespace UnitTest.VentasPago
{
    public class VentasPagoServiceTests
    {
        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            using var context = CreateContext();
            var cache = new FakeCacheService();

            Assert.Throws<ArgumentNullException>(() => new VentasPagoService(null!, cache, new FakePagoEventPublisher()));
            Assert.Throws<ArgumentNullException>(() => new VentasPagoService(context, null!, new FakePagoEventPublisher()));
            Assert.Throws<ArgumentNullException>(() => new VentasPagoService(context, cache, null!));
        }

        [Fact]
        public async Task NullDtoThrowsArgumentNull()
        {
            await using var context = CreateContext();
            var service = new VentasPagoService(context, new FakeCacheService(), new FakePagoEventPublisher());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
        }

        [Fact]
        public async Task UnknownPedidoThrowsValidationWithoutInsert()
        {
            await using var context = CreateContext();
            SeedBase(context);
            var service = new VentasPagoService(context, new FakeCacheService(), new FakePagoEventPublisher());

            var dto = new PagoCreateDto { idVenPedido = Guid.NewGuid(), decMonto = 10m };

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto));
            Assert.Equal($"Pedido '{dto.idVenPedido}' no existe.", ex.Message);
            Assert.Equal(0, await context.VenPedidoPagos.CountAsync());
        }

        [Fact]
        public async Task DuplicateTransaccionThrowsConflictWithoutSecondInsert()
        {
            await using var context = CreateContext();
            var pedidoId = SeedPedido(context);
            var service = new VentasPagoService(context, new FakeCacheService(), new FakePagoEventPublisher());

            await service.CreateAsync(new PagoCreateDto { idVenPedido = pedidoId, decMonto = 10m, strIdTransaccion = "TX-1" });

            var ex = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.CreateAsync(new PagoCreateDto { idVenPedido = pedidoId, decMonto = 10m, strIdTransaccion = "TX-1" }));
            Assert.Equal("La transacción 'TX-1' ya fue registrada.", ex.Message);
            Assert.Equal(1, await context.VenPedidoPagos.CountAsync());
        }

        [Fact]
        public async Task NullTransaccionAllowsDuplicates()
        {
            await using var context = CreateContext();
            var pedidoId = SeedPedido(context);
            var service = new VentasPagoService(context, new FakeCacheService(), new FakePagoEventPublisher());

            var first = await service.CreateAsync(new PagoCreateDto { idVenPedido = pedidoId, decMonto = 10m });
            var second = await service.CreateAsync(new PagoCreateDto { idVenPedido = pedidoId, decMonto = 5m });

            Assert.Null(first.strIdTransaccion);
            Assert.Null(second.strIdTransaccion);
            Assert.NotEqual(first.id, second.id);
            Assert.Equal(2, await context.VenPedidoPagos.CountAsync());
        }

        [Fact]
        public async Task BlankTransaccionNormalizedToNull()
        {
            await using var context = CreateContext();
            var pedidoId = SeedPedido(context);
            var service = new VentasPagoService(context, new FakeCacheService(), new FakePagoEventPublisher());

            var created = await service.CreateAsync(new PagoCreateDto { idVenPedido = pedidoId, decMonto = 10m, strIdTransaccion = "   " });

            Assert.Null(created.strIdTransaccion);
            Assert.Null((await context.VenPedidoPagos.SingleAsync()).strIdTransaccion);
        }

        [Fact]
        public async Task CreatePersistsEstadoFechaCachesAndVersion()
        {
            var cache = new FakeCacheService();
            var publisher = new FakePagoEventPublisher();
            await using var context = CreateContext();
            var pedidoId = SeedPedido(context);
            var service = new VentasPagoService(context, cache, publisher);

            var created = await service.CreateAsync(new PagoCreateDto { idVenPedido = pedidoId, decMonto = 99.99m, strMetodoPago = "  Efectivo ", strIdTransaccion = " TX-ABC " });

            Assert.NotEqual(0, created.id);
            Assert.Equal(pedidoId, created.idVenPedido);
            Assert.Equal(99.99m, created.decMonto);
            Assert.Equal("Efectivo", created.strMetodoPago);
            Assert.Equal("TX-ABC", created.strIdTransaccion);
            Assert.Equal("Procesado", created.strEstado);

            Assert.Equal(1, await cache.GetAsync<int>("cache:", "pago:version"));
            Assert.Contains(cache.Keys, key => key == "cache:pago:version");
            var fetched = await service.GetByIdAsync(created.id);
            Assert.NotNull(fetched);
            Assert.Equal(created.id, fetched!.id);
            Assert.Equal("Procesado", fetched.strEstado);
            Assert.Contains(cache.Keys, key => key == $"cache:pago:{created.id}");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
            var published = Assert.Single(publisher.Published);
            Assert.Equal(pedidoId, published.PedidoId);
            Assert.Equal("TX-ABC", published.IdTransaccion);
            Assert.Equal(99.99m, published.Monto);
        }

        [Fact]
        public async Task SecondCreateBumpsVersionToTwo()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var pedidoId = SeedPedido(context);
            var service = new VentasPagoService(context, cache, new FakePagoEventPublisher());

            await service.CreateAsync(new PagoCreateDto { idVenPedido = pedidoId, decMonto = 10m });
            await service.CreateAsync(new PagoCreateDto { idVenPedido = pedidoId, decMonto = 20m });

            Assert.Equal(2, await cache.GetAsync<int>("cache:", "pago:version"));
        }

        [Fact]
        public async Task GetByIdReturnsNullWhenMissingAndStaleCacheAfterDelete()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var pedidoId = SeedPedido(context);
            var service = new VentasPagoService(context, cache, new FakePagoEventPublisher());

            Assert.Null(await service.GetByIdAsync(999999));

            var created = await service.CreateAsync(new PagoCreateDto { idVenPedido = pedidoId, decMonto = 10m });
            var first = await service.GetByIdAsync(created.id);
            Assert.NotNull(first);

            context.VenPedidoPagos.RemoveRange(context.VenPedidoPagos);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            Assert.Equal(0, await context.VenPedidoPagos.CountAsync());

            var stale = await service.GetByIdAsync(created.id);
            Assert.NotNull(stale);
            Assert.Equal(created.id, stale!.id);
            Assert.Equal(10m, stale.decMonto);
        }

        [Fact]
        public async Task GetByPedidoIdReturnsOrderedAndServesCache()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var pedidoId = SeedPedido(context);
            var otherPedidoId = SeedPedido(context);
            var service = new VentasPagoService(context, cache, new FakePagoEventPublisher());

            var first = await service.CreateAsync(new PagoCreateDto { idVenPedido = pedidoId, decMonto = 10m, strIdTransaccion = "TX-A" });
            var second = await service.CreateAsync(new PagoCreateDto { idVenPedido = pedidoId, decMonto = 20m, strIdTransaccion = "TX-B" });
            await service.CreateAsync(new PagoCreateDto { idVenPedido = otherPedidoId, decMonto = 30m, strIdTransaccion = "TX-C" });

            var pagos = await service.GetByPedidoIdAsync(pedidoId);

            Assert.Equal(2, pagos.Count);
            Assert.Equal(first.id, pagos[0].id);
            Assert.Equal(second.id, pagos[1].id);
            Assert.Equal(10m, pagos[0].decMonto);
            Assert.Equal(20m, pagos[1].decMonto);

            context.VenPedidoPagos.RemoveRange(context.VenPedidoPagos);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var cached = await service.GetByPedidoIdAsync(pedidoId);
            Assert.Equal(2, cached.Count);
            Assert.Equal(first.id, cached[0].id);
        }

        [Fact]
        public async Task GetByPedidoIdEmptyWhenNoPagos()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var pedidoId = SeedPedido(context);
            var service = new VentasPagoService(context, cache, new FakePagoEventPublisher());

            var pagos = await service.GetByPedidoIdAsync(pedidoId);

            Assert.Empty(pagos);
            Assert.Contains(cache.Keys, key => key.StartsWith("cache:pago:pedido:", StringComparison.Ordinal));
        }

        [Fact]
        public void CreateValidatorRejectsInvalid()
        {
            var validator = new PagoCreateValidator();

            Assert.False(validator.Validate(new PagoCreateDto { idVenPedido = Guid.Empty, decMonto = 10m }).IsValid);
            Assert.False(validator.Validate(new PagoCreateDto { idVenPedido = Guid.NewGuid(), decMonto = 0m }).IsValid);
            Assert.False(validator.Validate(new PagoCreateDto { idVenPedido = Guid.NewGuid(), decMonto = -1m }).IsValid);
            Assert.False(validator.Validate(new PagoCreateDto { idVenPedido = Guid.NewGuid(), decMonto = 10m, strMetodoPago = new string('M', 51) }).IsValid);
            Assert.False(validator.Validate(new PagoCreateDto { idVenPedido = Guid.NewGuid(), decMonto = 10m, strIdTransaccion = new string('T', 101) }).IsValid);
            Assert.True(validator.Validate(new PagoCreateDto { idVenPedido = Guid.NewGuid(), decMonto = 10m }).IsValid);
        }

        [Fact]
        public async Task ConcurrencyExceptionMapsToConflict()
        {
            await using var context = CreateThrowingContext(new DbUpdateConcurrencyException("boom"));
            var pedidoId = SeedPedido(context);
            context.ThrowOnSave = true;
            var service = new VentasPagoService(context, new FakeCacheService(), new FakePagoEventPublisher());

            var ex = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.CreateAsync(new PagoCreateDto { idVenPedido = pedidoId, decMonto = 10m }));
            Assert.Equal("El pago fue modificado por otro proceso.", ex.Message);
        }

        [Fact]
        public async Task UpdateExceptionMapsToConflict()
        {
            await using var context = CreateThrowingContext(new DbUpdateException("boom", new InvalidOperationException("inner")));
            var pedidoId = SeedPedido(context);
            context.ThrowOnSave = true;
            var service = new VentasPagoService(context, new FakeCacheService(), new FakePagoEventPublisher());

            var ex = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.CreateAsync(new PagoCreateDto { idVenPedido = pedidoId, decMonto = 10m }));
            Assert.Equal("El pago fue modificado por otro proceso.", ex.Message);
        }

        private static Guid SeedPedido(AppDbContext context)
        {
            SeedBase(context);
            var pedidoId = Guid.NewGuid();
            context.VenPedidos.Add(new PedidoModel { id = pedidoId, idCliCliente = 1, dteFechaPedido = DateTime.UtcNow, decTotal = 10m, strEstadoSaga = "Creado" });
            context.SaveChanges();
            context.ChangeTracker.Clear();
            return pedidoId;
        }

        private static void SeedBase(AppDbContext context)
        {
            if (!context.CliClientes.Any())
            {
                context.CliClientes.Add(new ClienteModel { id = 1, strNombreCliente = "Ana", strCorreoElectronico = "a@test.local", strNumeroTelefono = "5550000001" });
                context.SaveChanges();
                context.ChangeTracker.Clear();
            }
        }

        private static AppDbContext CreateContext()
        {
            return new AppDbContext(CreateOptions());
        }

        private static DbContextOptions<AppDbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        private static ThrowingContext CreateThrowingContext(Exception toThrow)
        {
            return new ThrowingContext(CreateOptions(), toThrow) { ThrowOnSave = false };
        }

        private sealed class ThrowingContext : AppDbContext
        {
            private readonly Exception _toThrow;

            public ThrowingContext(DbContextOptions<AppDbContext> options, Exception toThrow)
                : base(options)
            {
                _toThrow = toThrow;
            }

            public bool ThrowOnSave { get; set; } = true;

            public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                if (ThrowOnSave)
                {
                    throw _toThrow;
                }

                return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private sealed class FakePagoEventPublisher : IPagoEventPublisher
        {
            private readonly List<PagoProcesadoEvent> _published = new();

            public IReadOnlyList<PagoProcesadoEvent> Published => _published;

            public Task PublishProcesadoAsync(PagoProcesadoEvent evt, CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(evt);
                _published.Add(evt);
                return Task.CompletedTask;
            }
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
