using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Consumers;
using WebAPIDevSecOpsScallingSDD.Events;

namespace UnitTest.Consumers
{
    public class FacturaConsumerTests
    {
        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            using var context = ConsumerSeeds.CreateContext();
            var cache = new FakeCacheService();
            var bus = new FakeEventBus();

            Assert.Throws<ArgumentNullException>(() => new FacturaConsumer(null!, cache, bus));
            Assert.Throws<ArgumentNullException>(() => new FacturaConsumer(context, null!, bus));
            Assert.Throws<ArgumentNullException>(() => new FacturaConsumer(context, cache, null!));
        }

        [Fact]
        public async Task NullMessageThrowsArgumentNull()
        {
            await using var context = ConsumerSeeds.CreateContext();
            var consumer = new FacturaConsumer(context, new FakeCacheService(), new FakeEventBus());

            await Assert.ThrowsAsync<ArgumentNullException>(() => consumer.HandleAsync(null!));
        }

        [Fact]
        public async Task UnknownPedidoSkipsWithoutPublish()
        {
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var bus = new FakeEventBus();
            var consumer = new FacturaConsumer(context, new FakeCacheService(), bus);

            await consumer.HandleAsync(new PagoProcesadoEvent { PedidoId = Guid.NewGuid(), Monto = 10m });

            Assert.Empty(bus.Published);
            Assert.Equal(0, await context.VenPedidoFacturas.CountAsync());
        }

        [Fact]
        public async Task HappyPathCreatesFacturaWithDeterministicFolio()
        {
            var cache = new FakeCacheService();
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "PagoProcesado");
            var consumer = new FacturaConsumer(context, cache, bus);
            await cache.SetAsync("cache:", $"pedido:{pedidoId}", "stale", TimeSpan.FromSeconds(60));

            await consumer.HandleAsync(new PagoProcesadoEvent { PedidoId = pedidoId, Monto = 20m });

            var factura = await context.VenPedidoFacturas.SingleAsync();
            var expectedFolio = $"F-{DateTime.UtcNow:yyyy}-{pedidoId:N}";
            Assert.Equal(expectedFolio, factura.strFolioFactura);
            Assert.Equal("Emitida", factura.strEstado);
            Assert.Equal(20m, factura.decTotal);
            Assert.Null(factura.strRFC);
            Assert.Equal("Facturado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            var published = Assert.Single(bus.Published);
            var generado = Assert.IsType<FacturaGeneradoEvent>(published);
            Assert.Equal(expectedFolio, generado.Folio);
            Assert.Null(await cache.GetAsync<string>("cache:", $"pedido:{pedidoId}"));
            Assert.Equal(1, await context.VenEventosProcesados.CountAsync(e => e.idPedido == pedidoId));
        }

        [Fact]
        public async Task EarlyStateThrowsForBusRetry()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "StockValidado");
            var consumer = new FacturaConsumer(context, new FakeCacheService(), bus);

            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.HandleAsync(new PagoProcesadoEvent { PedidoId = pedidoId, Monto = 20m }));

            Assert.Equal(0, await context.VenPedidoFacturas.CountAsync());
            Assert.Empty(bus.Published);
        }

        [Fact]
        public async Task EarlyStateThrowMentionsPedidoId()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "StockValidado");
            var consumer = new FacturaConsumer(context, new FakeCacheService(), bus);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.HandleAsync(new PagoProcesadoEvent { PedidoId = pedidoId, Monto = 20m }));

            Assert.Contains(pedidoId.ToString(), ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AlreadyMarkedSkipsWithoutCreating()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "PagoProcesado");
            EventIdempotency.MarkProcessed(context, "FacturaConsumer:PagoProcesadoEvent", pedidoId);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var consumer = new FacturaConsumer(context, new FakeCacheService(), bus);

            await consumer.HandleAsync(new PagoProcesadoEvent { PedidoId = pedidoId, Monto = 20m });

            Assert.Equal(0, await context.VenPedidoFacturas.CountAsync());
            Assert.Equal("PagoProcesado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            Assert.Empty(bus.Published);
        }

        [Fact]
        public async Task EarlyStateLeavesNoMarkSoRedeliveryRetries()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "StockValidado");
            var consumer = new FacturaConsumer(context, new FakeCacheService(), bus);
            var message = new PagoProcesadoEvent { PedidoId = pedidoId, Monto = 20m };

            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.HandleAsync(message));
            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.HandleAsync(message));

            Assert.Equal(0, await context.VenEventosProcesados.CountAsync(e => e.idPedido == pedidoId));
        }

        [Fact]
        public async Task WrongStatePublishesRechazada()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "Creado");
            var consumer = new FacturaConsumer(context, new FakeCacheService(), bus);

            await consumer.HandleAsync(new PagoProcesadoEvent { PedidoId = pedidoId, Monto = 20m });

            Assert.Equal(0, await context.VenPedidoFacturas.CountAsync());
            var published = Assert.Single(bus.Published);
            var rechazada = Assert.IsType<FacturaRechazadaEvent>(published);
            Assert.Equal(pedidoId, rechazada.PedidoId);
            Assert.Contains("Creado", rechazada.Motivo, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AlreadyFacturadoSkips()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "Facturado");
            var consumer = new FacturaConsumer(context, new FakeCacheService(), bus);

            await consumer.HandleAsync(new PagoProcesadoEvent { PedidoId = pedidoId, Monto = 20m });

            Assert.Equal(0, await context.VenPedidoFacturas.CountAsync());
            Assert.Empty(bus.Published);
        }

        [Fact]
        public async Task DuplicateFolioSkips()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "PagoProcesado");
            var folio = $"F-{DateTime.UtcNow:yyyy}-{pedidoId:N}";
            context.VenPedidoFacturas.Add(new WebAPIDevSecOpsScallingSDD.Models.VenPedidoFactura { idVenPedido = pedidoId, strFolioFactura = folio, decTotal = 20m, dteFechaEmision = DateTime.UtcNow, strEstado = "Emitida" });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var consumer = new FacturaConsumer(context, new FakeCacheService(), bus);

            await consumer.HandleAsync(new PagoProcesadoEvent { PedidoId = pedidoId, Monto = 20m });

            Assert.Equal(1, await context.VenPedidoFacturas.CountAsync());
            Assert.Empty(bus.Published);
        }
    }
}
