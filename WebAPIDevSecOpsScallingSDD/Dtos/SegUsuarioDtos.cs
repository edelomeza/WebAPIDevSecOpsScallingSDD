namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class SegUsuarioDto
    {
        public int id { get; set; }

        public string strNombre { get; set; } = string.Empty;

        public string strCorreoElectronico { get; set; } = string.Empty;

        public System.DateTime? dteFechaRegistro { get; set; }

        public bool bln2FAHabilitado { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }

    public sealed class SegUsuarioCreateDto
    {
        public required string strNombre { get; set; }

        public required string strCorreoElectronico { get; set; }

        public required string strPasswordPlano { get; set; }
    }

    public sealed class SegUsuarioUpdateDto
    {
        public required int id { get; set; }

        public string strNombre { get; set; } = string.Empty;

        public string strCorreoElectronico { get; set; } = string.Empty;

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }

    public sealed class SegUsuarioDeleteDto
    {
        public required int id { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }

    public sealed class SegUsuarioAutocompleteDto
    {
        public int id { get; set; }

        public string strNombre { get; set; } = string.Empty;
    }
}
