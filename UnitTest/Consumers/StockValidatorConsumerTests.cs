using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Consumers;
using WebAPIDevSecOpsScallingSDD.Events;
using WebAPIDevSecOpsScallingSDD.Services;

namespace UnitTest.Consumers
{
    public class StockValidatorConsumerTests
    {
        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            using var context = ConsumerSeeds.CreateContext();
            var cache = new FakeCacheService();
            var bus = new FakeEventBus();

            Assert.Throws<ArgumentNullException>(() => new StockValidatorConsumer(null!, cache, bus));
            Assert.Throws<ArgumentNullException>(() => new StockValidatorConsumer(context, null!, bus));
            Assert.Throws<ArgumentNullException>(() => new StockValidatorConsumer(context, cache, null!));
        }

        [Fact]
        public async Task NullMessageThrowsArgumentNull()
        {
            await using var context = ConsumerSeeds.CreateContext();
            var consumer = new StockValidatorConsumer(context, new FakeCacheService(), new FakeEventBus());

            await Assert.ThrowsAsync<ArgumentNullException>(() => consumer.HandleAsync(null!));
        }

        [Fact]
        public async Task UnknownPedidoSkipsWithoutPublish()
        {
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var bus = new FakeEventBus();
            var consumer = new StockValidatorConsumer(context, new FakeCacheService(), bus);

            await consumer.HandleAsync(new PedidoCreadoEvent { PedidoId = Guid.NewGuid(), ClienteId = 1, Total = 20m });

            Assert.Empty(bus.Published);
            Assert.Equal(0, await context.VenPedidos.CountAsync());
        }

        [Fact]
        public async Task WithStockValidatesReservesAndPublishes()
        {
            var cache = new FakeCacheService();
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context, existencia: 5, precio: 10m);
            var pedidoId = ConsumerSeeds.SeedPedido(context);
            var consumer = new StockValidatorConsumer(context, cache, bus);
            await cache.SetAsync("cache:", $"pedido:{pedidoId}", "stale", TimeSpan.FromSeconds(60));

            await consumer.HandleAsync(new PedidoCreadoEvent { PedidoId = pedidoId, ClienteId = 1, Total = 20m });

            Assert.Equal("StockValidado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            Assert.Equal(3, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
            var published = Assert.Single(bus.Published);
            var validado = Assert.IsType<StockValidadoEvent>(published);
            Assert.Equal(pedidoId, validado.PedidoId);
            Assert.Null(await cache.GetAsync<string>("cache:", $"pedido:{pedidoId}"));
        }

        [Fact]
        public async Task WithoutStockRejectsWithMotivo()
        {
            var cache = new FakeCacheService();
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context, existencia: 1, precio: 10m);
            var pedidoId = ConsumerSeeds.SeedPedido(context, cantidad: 2);
            var consumer = new StockValidatorConsumer(context, cache, bus);
            await cache.SetAsync("cache:", $"pedido:{pedidoId}", "stale", TimeSpan.FromSeconds(60));

            await consumer.HandleAsync(new PedidoCreadoEvent { PedidoId = pedidoId, ClienteId = 1, Total = 20m });

            var pedido = await context.VenPedidos.SingleAsync();
            Assert.Equal("StockRechazado", pedido.strEstadoSaga);
            Assert.Equal("Sin stock suficiente.", pedido.strMotivoRechazo);
            Assert.Equal(1, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
            var published = Assert.Single(bus.Published);
            var rechazado = Assert.IsType<StockRechazadoEvent>(published);
            Assert.Equal(pedidoId, rechazado.PedidoId);
            Assert.Equal("Sin stock suficiente.", rechazado.Motivo);
            Assert.Null(await cache.GetAsync<string>("cache:", $"pedido:{pedidoId}"));
            Assert.Equal(1, await context.VenEventosProcesados.CountAsync(e => e.idPedido == pedidoId));
        }

        [Fact]
        public async Task PartialStockRejectsWithoutReserving()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context, existencia: 5, precio: 10m);
            context.ProProductos.Add(new WebAPIDevSecOpsScallingSDD.Models.ProProducto { id = 2, strNombreProducto = "Tuerca", intNumeroExistencia = 1, decPrecio = 5m });
            var pedidoId = Guid.NewGuid();
            context.VenPedidos.Add(new WebAPIDevSecOpsScallingSDD.Models.VenPedido { id = pedidoId, idCliCliente = 1, dteFechaPedido = DateTime.UtcNow, decTotal = 25m, strEstadoSaga = "Creado" });
            context.VenPedidoDetalles.Add(new WebAPIDevSecOpsScallingSDD.Models.VenPedidoDetalle { idVenPedido = pedidoId, idProProducto = 1, intCantidad = 2, decPrecioUnitario = 10m });
            context.VenPedidoDetalles.Add(new WebAPIDevSecOpsScallingSDD.Models.VenPedidoDetalle { idVenPedido = pedidoId, idProProducto = 2, intCantidad = 2, decPrecioUnitario = 5m });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var consumer = new StockValidatorConsumer(context, new FakeCacheService(), bus);

            await consumer.HandleAsync(new PedidoCreadoEvent { PedidoId = pedidoId, ClienteId = 1, Total = 25m });

            Assert.Equal("StockRechazado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            Assert.Equal(5, (await context.ProProductos.SingleAsync(e => e.id == 1)).intNumeroExistencia);
            Assert.Single(bus.Published);
        }

        [Fact]
        public async Task AlreadyMarkedSkipsWithoutMutating()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context, existencia: 5, precio: 10m);
            var pedidoId = ConsumerSeeds.SeedPedido(context);
            EventIdempotency.MarkProcessed(context, "StockValidator:PedidoCreadoEvent", pedidoId);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var consumer = new StockValidatorConsumer(context, new FakeCacheService(), bus);

            await consumer.HandleAsync(new PedidoCreadoEvent { PedidoId = pedidoId, ClienteId = 1, Total = 20m });

            Assert.Equal("Creado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            Assert.Equal(5, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
            Assert.Empty(bus.Published);
        }

        [Fact]
        public async Task EmptyDetallesRejects()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = Guid.NewGuid();
            context.VenPedidos.Add(new WebAPIDevSecOpsScallingSDD.Models.VenPedido { id = pedidoId, idCliCliente = 1, dteFechaPedido = DateTime.UtcNow, decTotal = 0m, strEstadoSaga = "Creado" });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var consumer = new StockValidatorConsumer(context, new FakeCacheService(), bus);

            await consumer.HandleAsync(new PedidoCreadoEvent { PedidoId = pedidoId, ClienteId = 1, Total = 0m });

            Assert.Equal("StockRechazado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            Assert.Single(bus.Published);
        }

        [Fact]
        public async Task RedeliverySkipsSecondProcessing()
        {
            var bus = new FakeEventBus();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context, existencia: 5, precio: 10m);
            var pedidoId = ConsumerSeeds.SeedPedido(context);
            var consumer = new StockValidatorConsumer(context, new FakeCacheService(), bus);
            var message = new PedidoCreadoEvent { PedidoId = pedidoId, ClienteId = 1, Total = 20m };

            await consumer.HandleAsync(message);
            await consumer.HandleAsync(message);

            Assert.Single(bus.Published);
            Assert.Equal(3, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
            Assert.Equal(1, await context.VenEventosProcesados.CountAsync(e => e.idPedido == pedidoId));
        }
    }
}
