using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Events;
using WebAPIDevSecOpsScallingSDD.Services;

namespace WebAPIDevSecOpsScallingSDD.Consumers
{
    // 06-01: valida el cobro publicado por POST /ventas/pago; estado inesperado → PagoRechazadoEvent.
    public sealed class PagoConsumer : IConsumer<PagoProcesadoEvent>
    {
        private const string CachePrefix = "cache:";

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;
        private readonly IEventBus _bus;

        public PagoConsumer(AppDbContext db, ICacheService cache, IEventBus bus)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(bus);
            _db = db;
            _cache = cache;
            _bus = bus;
        }

        public Task Consume(ConsumeContext<PagoProcesadoEvent> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return HandleAsync(context.Message, context.CancellationToken);
        }

        internal async Task HandleAsync(PagoProcesadoEvent message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            var pedido = await _db.VenPedidos.FirstOrDefaultAsync(e => e.id == message.PedidoId, cancellationToken).ConfigureAwait(false);
            if (pedido is null || string.Equals(pedido.strEstadoSaga, SagaStates.PagoProcesado, StringComparison.Ordinal) || string.Equals(pedido.strEstadoSaga, SagaStates.Facturado, StringComparison.Ordinal))
            {
                return;
            }

            if (!string.Equals(pedido.strEstadoSaga, SagaStates.StockValidado, StringComparison.Ordinal))
            {
                // Rechazo: se publica antes de mutar para no perderlo; un duplicado es inocuo
                // porque la compensación es idempotente por (evento, pedido).
                await _bus.PublishAsync(new PagoRechazadoEvent { PedidoId = message.PedidoId, Motivo = $"El pedido está en estado '{pedido.strEstadoSaga}' y no admite cobro." }, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (await EventIdempotency.IsProcessedAsync(_db, "PagoConsumer:PagoProcesadoEvent", message.PedidoId, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            pedido.strEstadoSaga = SagaStates.PagoProcesado;
            EventIdempotency.MarkProcessed(_db, "PagoConsumer:PagoProcesadoEvent", message.PedidoId);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _cache.RemoveAsync(CachePrefix, $"pedido:{message.PedidoId}", cancellationToken).ConfigureAwait(false);
        }
    }
}
