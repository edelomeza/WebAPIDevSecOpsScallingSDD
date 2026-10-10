using System;

namespace WebAPIDevSecOpsScallingSDD.Events
{
    // Contrato 06-03 v1: sin stock suficiente; dispara compensación (restaurar + cancelar).
    public sealed record StockRechazadoEvent
    {
        public const int SchemaVersion = 1;

        public Guid PedidoId { get; init; }

        public string Motivo { get; init; } = string.Empty;
    }
}
