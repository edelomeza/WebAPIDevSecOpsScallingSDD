namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class ProProductoDto
    {
        public int id { get; set; }

        public string strNombreProducto { get; set; } = string.Empty;

        public string? strURLImagen { get; set; }

        public string? strDescripcion { get; set; }

        public int intNumeroExistencia { get; set; }

        public decimal decPrecio { get; set; }

        public string? strCreadoPorUsuario { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }

    public sealed class ProProductoCreateDto
    {
        public string strNombreProducto { get; set; } = string.Empty;

        public string? strURLImagen { get; set; }

        public string? strDescripcion { get; set; }

        public required int intNumeroExistencia { get; set; }

        public required decimal decPrecio { get; set; }

        public string? strCreadoPorUsuario { get; set; }
    }

    public sealed class ProProductoUpdateDto
    {
        public required int id { get; set; }

        public string strNombreProducto { get; set; } = string.Empty;

        public string? strURLImagen { get; set; }

        public string? strDescripcion { get; set; }

        public required int intNumeroExistencia { get; set; }

        public required decimal decPrecio { get; set; }

        public string? strCreadoPorUsuario { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }

    public sealed class ProProductoDeleteDto
    {
        public required int id { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }

    public sealed class ProProductoAutocompleteDto
    {
        public int id { get; set; }

        public string strNombreProducto { get; set; } = string.Empty;
    }
}
