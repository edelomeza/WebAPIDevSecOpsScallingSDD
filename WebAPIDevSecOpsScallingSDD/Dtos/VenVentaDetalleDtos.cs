namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class VenVentaDetalleCreateDto
    {
        public required int idProProducto { get; set; }

        public required int intPiezaVenta { get; set; }
    }

    public sealed class VenVentaDetalleDeleteDto
    {
        public required int id { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }
}
