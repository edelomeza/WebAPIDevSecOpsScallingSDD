using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Events;
using WebAPIDevSecOpsScallingSDD.Services;

namespace WebAPIDevSecOpsScallingSDD.Consumers
{
    // Compensación mínima de 06-01. Los dos niveles completos llegan en 06-02. Aquí se
    // anulan los pagos y se devuelve el stock que la validación había reservado.
    public sealed class CompensationConsumer :
        IConsumer<StockRechazadoEvent>,
        IConsumer<PagoRechazadoEvent>,
        IConsumer<FacturaRechazadaEvent>
    {
        private const string CachePrefix = "cache:";

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;

        public CompensationConsumer(AppDbContext db, ICacheService cache)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            _db = db;
            _cache = cache;
        }

        public Task Consume(ConsumeContext<StockRechazadoEvent> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return HandleAsync(context.Message.PedidoId, context.Message.Motivo, "Compensation:StockRechazadoEvent", context.CancellationToken);
        }

        public Task Consume(ConsumeContext<PagoRechazadoEvent> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return HandleAsync(context.Message.PedidoId, context.Message.Motivo, "Compensation:PagoRechazadoEvent", context.CancellationToken);
        }

        public Task Consume(ConsumeContext<FacturaRechazadaEvent> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return HandleAsync(context.Message.PedidoId, context.Message.Motivo, "Compensation:FacturaRechazadaEvent", context.CancellationToken);
        }

        internal async Task HandleAsync(Guid pedidoId, string motivo, string evento, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
            ArgumentException.ThrowIfNullOrWhiteSpace(evento);

            var pedido = await _db.VenPedidos.FirstOrDefaultAsync(e => e.id == pedidoId, cancellationToken).ConfigureAwait(false);
            if (pedido is null || string.Equals(pedido.strEstadoSaga, SagaStates.Cancelado, StringComparison.Ordinal))
            {
                return;
            }

            if (await EventIdempotency.IsProcessedAsync(_db, evento, pedidoId, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var reserved = !string.Equals(pedido.strEstadoSaga, SagaStates.StockRechazado, StringComparison.Ordinal);
            if (reserved)
            {
                var detalles = await _db.VenPedidoDetalles.Where(d => d.idVenPedido == pedidoId).ToListAsync(cancellationToken).ConfigureAwait(false);
                var productIds = detalles.Select(d => d.idProProducto).Distinct().ToList();
                var productos = await _db.ProProductos.Where(e => productIds.Contains(e.id)).ToDictionaryAsync(e => e.id, cancellationToken).ConfigureAwait(false);
                foreach (var item in detalles)
                {
                    if (productos.TryGetValue(item.idProProducto, out var producto))
                    {
                        producto.intNumeroExistencia += item.intCantidad;
                    }
                }

                var pagos = await _db.VenPedidoPagos.Where(e => e.idVenPedido == pedidoId).ToListAsync(cancellationToken).ConfigureAwait(false);
                foreach (var pago in pagos)
                {
                    pago.strEstado = "Anulado";
                }

                foreach (var productId in productIds)
                {
                    await _cache.RemoveAsync(CachePrefix, $"producto:{productId}", cancellationToken).ConfigureAwait(false);
                }

                var version = await _cache.GetAsync<int>(CachePrefix, "producto:version", cancellationToken).ConfigureAwait(false);
                await _cache.SetAsync(CachePrefix, "producto:version", version + 1, TimeSpan.FromSeconds(60), cancellationToken).ConfigureAwait(false);
            }

            pedido.strEstadoSaga = SagaStates.Cancelado;
            pedido.strMotivoRechazo = motivo.Length > 500 ? motivo.Substring(0, 500) : motivo;
            EventIdempotency.MarkProcessed(_db, evento, pedidoId);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _cache.RemoveAsync(CachePrefix, $"pedido:{pedidoId}", cancellationToken).ConfigureAwait(false);
        }
    }
}
