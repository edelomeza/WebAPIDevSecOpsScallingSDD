using System;

namespace WebAPIDevSecOpsScallingSDD.Events
{
    // Contrato 06-03 v1: el stock del pedido alcanza para todos sus detalles.
    public sealed record StockValidadoEvent
    {
        public const int SchemaVersion = 1;

        public Guid PedidoId { get; init; }
    }
}
