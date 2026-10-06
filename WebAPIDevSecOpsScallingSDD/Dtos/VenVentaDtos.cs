using System.Collections.Generic;

namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class VenVentaDetalleDto
    {
        public int id { get; set; }

        public int idProProducto { get; set; }

        public int intPiezaVenta { get; set; }

        public decimal decTotalVenta { get; set; }
    }

    public sealed class VenVentaDto
    {
        public int id { get; set; }

        public int idCliCliente { get; set; }

        public int idSegUsuario { get; set; }

        public int idVenCatEstado { get; set; }

        public System.DateTime? dteFechaHoraCompra { get; set; }

        public string strClaveVenta { get; set; } = string.Empty;

        public IReadOnlyList<VenVentaDetalleDto> Detalles { get; set; } = [];

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }

    public sealed class VenVentaDetalleCreateItemDto
    {
        public required int idProProducto { get; set; }

        public required int intPiezaVenta { get; set; }
    }

    public sealed class VenVentaCreateDto
    {
        public required int idCliCliente { get; set; }

        // NOTE (04-01): legacy recibe el dueño en el body; migrar a claim sub del JWT.
        public required int idSegUsuario { get; set; }

        public required int idVenCatEstado { get; set; }

        public string strClaveVenta { get; set; } = string.Empty;

        public IReadOnlyList<VenVentaDetalleCreateItemDto> Detalles { get; set; } = [];
    }
}
