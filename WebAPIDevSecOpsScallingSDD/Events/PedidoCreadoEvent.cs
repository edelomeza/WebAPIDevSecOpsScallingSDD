using System;

namespace WebAPIDevSecOpsScallingSDD.Events
{
    // NOTE (06-03): schema temporal del evento inicial de la saga; 06-03 fija el contrato definitivo.
    public sealed class PedidoCreadoEvent
    {
        public Guid PedidoId { get; set; }

        public int ClienteId { get; set; }

        public decimal Total { get; set; }
    }
}
