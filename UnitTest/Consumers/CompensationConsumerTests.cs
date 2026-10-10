using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Consumers;
using WebAPIDevSecOpsScallingSDD.Events;
using PagoModel = WebAPIDevSecOpsScallingSDD.Models.VenPedidoPago;

namespace UnitTest.Consumers
{
    public class CompensationConsumerTests
    {
        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            using var context = ConsumerSeeds.CreateContext();
            var cache = new FakeCacheService();

            Assert.Throws<ArgumentNullException>(() => new CompensationConsumer(null!, cache));
            Assert.Throws<ArgumentNullException>(() => new CompensationConsumer(context, null!));
        }

        [Fact]
        public async Task StockRechazadoCancelsWithoutRestoring()
        {
            var cache = new FakeCacheService();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context, existencia: 1, precio: 10m);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "StockRechazado", cantidad: 2);
            var consumer = new CompensationConsumer(context, cache);
            await cache.SetAsync("cache:", $"pedido:{pedidoId}", "stale", TimeSpan.FromSeconds(60));

            await consumer.HandleAsync(pedidoId, "Sin stock suficiente.", "Compensation:StockRechazadoEvent");

            Assert.Equal("Cancelado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            Assert.Equal("Sin stock suficiente.", (await context.VenPedidos.SingleAsync()).strMotivoRechazo);
            Assert.Equal(1, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
            Assert.Null(await cache.GetAsync<string>("cache:", $"pedido:{pedidoId}"));
        }

        [Fact]
        public async Task PagoRechazadoRestoresStockAndVoidsPago()
        {
            var cache = new FakeCacheService();
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context, existencia: 3, precio: 10m);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "StockValidado", cantidad: 2);
            context.VenPedidoPagos.Add(new PagoModel { idVenPedido = pedidoId, decMonto = 20m, strEstado = "Procesado", dteFechaPago = DateTime.UtcNow });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var consumer = new CompensationConsumer(context, cache);
            await cache.SetAsync("cache:", "producto:1", "stale", TimeSpan.FromSeconds(60));
            await cache.SetAsync("cache:", "producto:version", 41, TimeSpan.FromSeconds(60));

            await consumer.HandleAsync(pedidoId, "Cobro inválido.", "Compensation:PagoRechazadoEvent");

            Assert.Equal("Cancelado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            Assert.Equal(5, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
            Assert.Equal("Anulado", (await context.VenPedidoPagos.SingleAsync()).strEstado);
            Assert.Null(await cache.GetAsync<string>("cache:", "producto:1"));
            Assert.Equal(42, await cache.GetAsync<int>("cache:", "producto:version"));
        }

        [Fact]
        public async Task FacturaRechazadaRestoresAndVoids()
        {
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context, existencia: 3, precio: 10m);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "PagoProcesado", cantidad: 2);
            context.VenPedidoPagos.Add(new PagoModel { idVenPedido = pedidoId, decMonto = 20m, strEstado = "Procesado", dteFechaPago = DateTime.UtcNow });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var consumer = new CompensationConsumer(context, new FakeCacheService());

            await consumer.HandleAsync(pedidoId, "Folio duplicado.", "Compensation:FacturaRechazadaEvent");

            Assert.Equal("Cancelado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            Assert.Equal(5, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
            Assert.Equal("Anulado", (await context.VenPedidoPagos.SingleAsync()).strEstado);
        }

        [Fact]
        public async Task UnknownPedidoSkips()
        {
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var consumer = new CompensationConsumer(context, new FakeCacheService());

            await consumer.HandleAsync(Guid.NewGuid(), "Motivo.", "Compensation:PagoRechazadoEvent");

            Assert.Equal(0, await context.VenPedidos.CountAsync());
        }

        [Fact]
        public async Task AlreadyCancelledSkips()
        {
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context, existencia: 3, precio: 10m);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "Cancelado", cantidad: 2);
            var consumer = new CompensationConsumer(context, new FakeCacheService());

            await consumer.HandleAsync(pedidoId, "Otro motivo.", "Compensation:PagoRechazadoEvent");

            Assert.Equal(3, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
        }

        [Fact]
        public async Task RedeliveryRestoresOnlyOnce()
        {
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context, existencia: 3, precio: 10m);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "StockValidado", cantidad: 2);
            var consumer = new CompensationConsumer(context, new FakeCacheService());

            await consumer.HandleAsync(pedidoId, "Cobro inválido.", "Compensation:PagoRechazadoEvent");
            await consumer.HandleAsync(pedidoId, "Cobro inválido.", "Compensation:PagoRechazadoEvent");

            Assert.Equal(5, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
            Assert.Equal(1, await context.VenEventosProcesados.CountAsync(e => e.idPedido == pedidoId));
        }

        [Fact]
        public async Task AlreadyMarkedSkipsWithoutMutating()
        {
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context, existencia: 3, precio: 10m);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "StockValidado", cantidad: 2);
            EventIdempotency.MarkProcessed(context, "Compensation:PagoRechazadoEvent", pedidoId);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var consumer = new CompensationConsumer(context, new FakeCacheService());

            await consumer.HandleAsync(pedidoId, "Cobro inválido.", "Compensation:PagoRechazadoEvent");

            Assert.Equal("StockValidado", (await context.VenPedidos.SingleAsync()).strEstadoSaga);
            Assert.Equal(3, (await context.ProProductos.SingleAsync()).intNumeroExistencia);
        }

        [Fact]
        public async Task LongMotivoTruncatesToColumnLimit()
        {
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "StockRechazado");
            var consumer = new CompensationConsumer(context, new FakeCacheService());

            await consumer.HandleAsync(pedidoId, new string('X', 501), "Compensation:StockRechazadoEvent");

            Assert.Equal(500, (await context.VenPedidos.SingleAsync()).strMotivoRechazo!.Length);
        }

        [Fact]
        public async Task ExactLimitMotivoIsKeptVerbatim()
        {
            await using var context = ConsumerSeeds.CreateContext();
            ConsumerSeeds.SeedBase(context);
            var pedidoId = ConsumerSeeds.SeedPedido(context, estado: "StockRechazado");
            var consumer = new CompensationConsumer(context, new FakeCacheService());
            var motivo = new string('Y', 500);

            await consumer.HandleAsync(pedidoId, motivo, "Compensation:StockRechazadoEvent");

            Assert.Equal(motivo, (await context.VenPedidos.SingleAsync()).strMotivoRechazo);
        }
    }
}
