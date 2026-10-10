using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Consumers;
using WebAPIDevSecOpsScallingSDD.Events;

namespace UnitTest.Consumers
{
    public class PagoConsumerTests
    {
        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            using var context = ConsumerSeeds.CreateContext();
            var cache = new FakeCacheService();
            var bus = new FakeEventBus();

            Assert.Throws<ArgumentNullException>(() => new PagoConsumer(null!, cache, bus));
            Assert.Throws<ArgumentNullException>(() => new PagoConsumer(context, null!, bus));
            Assert.Throws<ArgumentNullException>(() => new PagoConsumer(context, cache, null!));
        }

        [Fact]
        public async Task NullMessageThrowsArgumentNull()
        {
            await using var context = ConsumerSeeds.CreateContext();
            var consumer = new PagoConsumer(context, new FakeCacheService(), new FakeEventBus());

            await Assert.ThrowsAsync<ArgumentNullException>(() => consumer.HandleAsync(null!));
        }

        [Fact]
        public async Task UnknownPedidoSkipsWithoutPublish()
        {
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var bus = new FakeEventBus();
            var consumer = new PagoConsumer(context, new FakeCacheService(), bus);

            await consumer.HandleAsync(new PagoProcesadoEvent { PedidoId = Guid.NewGuid(), Monto = 10m });

            Assert.Empty(bus.Published);
        }

        [Fact]
        public async Task StockValidadoAdvancesWithoutPublish()
        {
            var cache = new FakeCacheService();
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "StockValidado");
            var consumer = new PagoConsumer(context, cache, bus);
            await cache.SetAsync("cache:", $"pedido:{pedidoId}", "stale", TimeSpan.FromSeconds(60));

            await consumer.HandleAsync(new PagoProcesadoEvent { PedidoId = pedidoId, IdTransaccion = "TX-1", Monto = 20m });

            Assert.Equal("PagoProcesado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            Assert.Empty(bus.Published);
            Assert.Null(await cache.GetAsync<string>("cache:", $"pedido:{pedidoId}"));
        }

        [Fact]
        public async Task WrongStatePublishesRechazado()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "Creado");
            var consumer = new PagoConsumer(context, new FakeCacheService(), bus);

            await consumer.HandleAsync(new PagoProcesadoEvent { PedidoId = pedidoId, Monto = 20m });

            Assert.Equal("Creado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            var published = Assert.Single(bus.Published);
            var rechazado = Assert.IsType<PagoRechazadoEvent>(published);
            Assert.Equal(pedidoId, rechazado.PedidoId);
            Assert.Contains("Creado", rechazado.Motivo, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AlreadyPaidSkips()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "PagoProcesado");
            var consumer = new PagoConsumer(context, new FakeCacheService(), bus);

            await consumer.HandleAsync(new PagoProcesadoEvent { PedidoId = pedidoId, Monto = 20m });

            Assert.Equal("PagoProcesado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            Assert.Empty(bus.Published);
        }

        [Fact]
        public async Task RedeliverySkipsSecondProcessing()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "StockValidado");
            var consumer = new PagoConsumer(context, new FakeCacheService(), bus);
            var message = new PagoProcesadoEvent { PedidoId = pedidoId, Monto = 20m };

            await consumer.HandleAsync(message);
            await consumer.HandleAsync(message);

            Assert.Equal("PagoProcesado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            Assert.Empty(bus.Published);
            Assert.Equal(1, await context.VenEventosProcesados.CountAsync(e => e.idPedido == pedidoId));
        }

        [Fact]
        public async Task AlreadyMarkedSkipsWithoutAdvancing()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "StockValidado");
            EventIdempotency.MarkProcessed(context, "PagoConsumer:PagoProcesadoEvent", pedidoId);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var consumer = new PagoConsumer(context, new FakeCacheService(), bus);

            await consumer.HandleAsync(new PagoProcesadoEvent { PedidoId = pedidoId, Monto = 20m });

            Assert.Equal("StockValidado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            Assert.Empty(bus.Published);
        }
    }
}
