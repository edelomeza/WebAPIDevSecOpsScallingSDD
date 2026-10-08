using System;

namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class PagoCreateDto
    {
        public required Guid idVenPedido { get; set; }

        public required decimal decMonto { get; set; }

        public string? strMetodoPago { get; set; }

        public string? strIdTransaccion { get; set; }
    }

    public sealed class PagoResponseDto
    {
        public int id { get; set; }

        public Guid idVenPedido { get; set; }

        public decimal decMonto { get; set; }

        public string? strMetodoPago { get; set; }

        public string? strIdTransaccion { get; set; }

        public string strEstado { get; set; } = string.Empty;

        public DateTime dteFechaPago { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }
}
