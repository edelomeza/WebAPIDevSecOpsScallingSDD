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
    // 06-01: valida y RESERVA el stock del pedido (descuenta existencias); sin stock → StockRechazadoEvent.
    public sealed class StockValidatorConsumer : IConsumer<PedidoCreadoEvent>
    {
        private const string CachePrefix = "cache:";

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;
        private readonly IEventBus _bus;

        public StockValidatorConsumer(AppDbContext db, ICacheService cache, IEventBus bus)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(bus);
            _db = db;
            _cache = cache;
            _bus = bus;
        }

        public Task Consume(ConsumeContext<PedidoCreadoEvent> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return HandleAsync(context.Message, context.CancellationToken);
        }

        internal async Task HandleAsync(PedidoCreadoEvent message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            var pedido = await _db.VenPedidos.FirstOrDefaultAsync(e => e.id == message.PedidoId, cancellationToken).ConfigureAwait(false);
            if (pedido is null)
            {
                return;
            }

            if (await EventIdempotency.IsProcessedAsync(_db, "StockValidator:PedidoCreadoEvent", message.PedidoId, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var detalles = await _db.VenPedidoDetalles.Where(d => d.idVenPedido == message.PedidoId).ToListAsync(cancellationToken).ConfigureAwait(false);
            var productIds = detalles.Select(d => d.idProProducto).Distinct().ToList();
            var productos = await _db.ProProductos.Where(e => productIds.Contains(e.id)).ToDictionaryAsync(e => e.id, cancellationToken).ConfigureAwait(false);
            var withStock = detalles.Count != 0 && detalles.All(d => productos.TryGetValue(d.idProProducto, out var producto) && producto.intNumeroExistencia >= d.intCantidad);

            if (withStock)
            {
                foreach (var item in detalles)
                {
                    productos[item.idProProducto].intNumeroExistencia -= item.intCantidad;
                }

                pedido.strEstadoSaga = SagaStates.StockValidado;
                EventIdempotency.MarkProcessed(_db, "StockValidator:PedidoCreadoEvent", message.PedidoId);
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await _cache.RemoveAsync(CachePrefix, $"pedido:{message.PedidoId}", cancellationToken).ConfigureAwait(false);
                await _bus.PublishAsync(new StockValidadoEvent { PedidoId = message.PedidoId }, cancellationToken).ConfigureAwait(false);
                return;
            }

            pedido.strEstadoSaga = SagaStates.StockRechazado;
            pedido.strMotivoRechazo = "Sin stock suficiente.";
            EventIdempotency.MarkProcessed(_db, "StockValidator:PedidoCreadoEvent", message.PedidoId);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _cache.RemoveAsync(CachePrefix, $"pedido:{message.PedidoId}", cancellationToken).ConfigureAwait(false);
            await _bus.PublishAsync(new StockRechazadoEvent { PedidoId = message.PedidoId, Motivo = "Sin stock suficiente." }, cancellationToken).ConfigureAwait(false);
        }
    }
}
