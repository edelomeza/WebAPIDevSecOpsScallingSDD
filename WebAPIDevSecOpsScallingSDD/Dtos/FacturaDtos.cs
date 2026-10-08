using System;

namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class VenPedidoFacturaResponseDto
    {
        public int id { get; set; }

        public Guid idVenPedido { get; set; }

        public string strFolioFactura { get; set; } = string.Empty;

        public string? strRFC { get; set; }

        public decimal decTotal { get; set; }

        public DateTime dteFechaEmision { get; set; }

        public string strEstado { get; set; } = string.Empty;

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }
}
