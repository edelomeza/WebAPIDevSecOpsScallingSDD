using System;
using System.Collections.Generic;

namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class PedidoDetalleCreateDto
    {
        public required int idProProducto { get; set; }

        public required int intCantidad { get; set; }
    }

    public sealed class PedidoCreateDto
    {
        public required int idCliCliente { get; set; }

        public IReadOnlyList<PedidoDetalleCreateDto> Detalles { get; set; } = [];
    }

    public sealed class PedidoDetalleResponseDto
    {
        public int id { get; set; }

        public int idProProducto { get; set; }

        public int intCantidad { get; set; }

        public decimal decPrecioUnitario { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }

    public sealed class PedidoResponseDto
    {
        public Guid id { get; set; }

        public int idCliCliente { get; set; }

        public DateTime dteFechaPedido { get; set; }

        public decimal decTotal { get; set; }

        public string strEstadoSaga { get; set; } = string.Empty;

        public IReadOnlyList<PedidoDetalleResponseDto> Detalles { get; set; } = [];

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }
}
