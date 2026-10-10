using System;

namespace WebAPIDevSecOpsScallingSDD.Events
{
    // Contrato 06-03 v1: la factura no pudo emitirse; dispara compensación 2 niveles (anular pago + restaurar stock).
    public sealed record FacturaRechazadaEvent
    {
        public const int SchemaVersion = 1;

        public Guid PedidoId { get; init; }

        public string Motivo { get; init; } = string.Empty;
    }
}
