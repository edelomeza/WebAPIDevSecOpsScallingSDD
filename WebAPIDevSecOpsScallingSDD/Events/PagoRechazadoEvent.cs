using System;

namespace WebAPIDevSecOpsScallingSDD.Events
{
    // Contrato 06-03 v1: el cobro no es válido; dispara compensación (restaurar stock + cancelar).
    public sealed record PagoRechazadoEvent
    {
        public const int SchemaVersion = 1;

        public Guid PedidoId { get; init; }

        public string Motivo { get; init; } = string.Empty;
    }
}
