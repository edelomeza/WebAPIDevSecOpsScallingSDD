namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class VenCatEstadoDto
    {
        public int id { get; set; }

        public string strValor { get; set; } = string.Empty;

        public string? strDescripcion { get; set; }
    }

    public sealed class VenCatEstadoCreateDto
    {
        public string strValor { get; set; } = string.Empty;

        public string? strDescripcion { get; set; }
    }

    public sealed class VenCatEstadoUpdateDto
    {
        public required int id { get; set; }

        public string strValor { get; set; } = string.Empty;

        public string? strDescripcion { get; set; }
    }

    public sealed class VenCatEstadoDeleteDto
    {
        public required int id { get; set; }
    }
}
