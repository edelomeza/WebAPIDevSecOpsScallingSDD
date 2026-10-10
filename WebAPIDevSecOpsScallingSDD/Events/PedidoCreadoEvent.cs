using System;

namespace WebAPIDevSecOpsScallingSDD.Events
{
    // Contrato 06-03 v1 (desviación registrada en spec: PedidoId es Guid — el PK real de VenPedido —
    // en vez de int; el pedido lleva N detalles, por eso viajan ClienteId+Total en vez de productoId/cantidad).
    public sealed record PedidoCreadoEvent
    {
        public const int SchemaVersion = 1;

        public Guid PedidoId { get; init; }

        public int ClienteId { get; init; }

        public decimal Total { get; init; }
    }
}
