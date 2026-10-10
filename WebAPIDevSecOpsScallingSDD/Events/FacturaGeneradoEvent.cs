using System;

namespace WebAPIDevSecOpsScallingSDD.Events
{
    // Contrato 06-03 v1: la factura quedó emitida con folio determinista del pedido.
    public sealed record FacturaGeneradoEvent
    {
        public const int SchemaVersion = 1;

        public Guid PedidoId { get; init; }

        public string Folio { get; init; } = string.Empty;
    }
}
