using System;

namespace WebAPIDevSecOpsScallingSDD.Events
{
    // Contrato 06-03 v1: el cobro quedó registrado (vía POST /ventas/pago).
    public sealed record PagoProcesadoEvent
    {
        public const int SchemaVersion = 1;

        public Guid PedidoId { get; init; }

        public string? IdTransaccion { get; init; }

        public decimal Monto { get; init; }
    }
}
