using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Events;
using WebAPIDevSecOpsScallingSDD.Models;
using WebAPIDevSecOpsScallingSDD.Services;

namespace WebAPIDevSecOpsScallingSDD.Consumers
{
    // 06-01: emite la factura del pedido cobrado con folio determinista (sin Increment atómico:
    // el folio deriva del pedido, único por construcción + UNIQUE como guardián).
    public sealed class FacturaConsumer : IConsumer<PagoProcesadoEvent>
    {
        private const string CachePrefix = "cache:";

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;
        private readonly IEventBus _bus;

        public FacturaConsumer(AppDbContext db, ICacheService cache, IEventBus bus)
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
            if (pedido is null || string.Equals(pedido.strEstadoSaga, SagaStates.Facturado, StringComparison.Ordinal) || string.Equals(pedido.strEstadoSaga, SagaStates.Cancelado, StringComparison.Ordinal))
            {
                return;
            }

            if (string.Equals(pedido.strEstadoSaga, SagaStates.StockValidado, StringComparison.Ordinal))
            {
                // Fuera de orden (el cobro aún no fue validado): reintento del bus SIN dejar marca,
                // para que el redelivery vuelva a entrar.
                throw new InvalidOperationException($"El pedido '{message.PedidoId}' aún no admite factura.");
            }

            if (!string.Equals(pedido.strEstadoSaga, SagaStates.PagoProcesado, StringComparison.Ordinal))
            {
                await _bus.PublishAsync(new FacturaRechazadaEvent { PedidoId = message.PedidoId, Motivo = $"El pedido está en estado '{pedido.strEstadoSaga}' y no admite factura." }, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (await EventIdempotency.IsProcessedAsync(_db, "FacturaConsumer:PagoProcesadoEvent", message.PedidoId, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var folio = $"F-{DateTime.UtcNow.ToString("yyyy", CultureInfo.InvariantCulture)}-{message.PedidoId:N}";
            if (await _db.VenPedidoFacturas.AsNoTracking().AnyAsync(e => e.strFolioFactura == folio, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            _db.VenPedidoFacturas.Add(new VenPedidoFactura
            {
                idVenPedido = message.PedidoId,
                strFolioFactura = folio,
                strRFC = null,
                decTotal = pedido.decTotal,
                dteFechaEmision = DateTime.UtcNow,
                strEstado = "Emitida",
            });
            pedido.strEstadoSaga = SagaStates.Facturado;
            EventIdempotency.MarkProcessed(_db, "FacturaConsumer:PagoProcesadoEvent", message.PedidoId);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _cache.RemoveAsync(CachePrefix, $"pedido:{message.PedidoId}", cancellationToken).ConfigureAwait(false);
            await _bus.PublishAsync(new FacturaGeneradoEvent { PedidoId = message.PedidoId, Folio = folio }, cancellationToken).ConfigureAwait(false);
        }
    }
}
