namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class CliClienteDto
    {
        public int id { get; set; }

        public string strNombreCliente { get; set; } = string.Empty;

        public string? strDireccionCliente { get; set; }

        public string strCorreoElectronico { get; set; } = string.Empty;

        public string strNumeroTelefono { get; set; } = string.Empty;

        public string? strCreadoPorUsuario { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }

    public sealed class CliClienteCreateDto
    {
        public string strNombreCliente { get; set; } = string.Empty;

        public string? strDireccionCliente { get; set; }

        public string strCorreoElectronico { get; set; } = string.Empty;

        public string strNumeroTelefono { get; set; } = string.Empty;

        public string? strCreadoPorUsuario { get; set; }
    }

    public sealed class CliClienteUpdateDto
    {
        public required int id { get; set; }

        public string strNombreCliente { get; set; } = string.Empty;

        public string? strDireccionCliente { get; set; }

        public string strCorreoElectronico { get; set; } = string.Empty;

        public string strNumeroTelefono { get; set; } = string.Empty;

        public string? strCreadoPorUsuario { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }

    public sealed class CliClienteDeleteDto
    {
        public required int id { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }

    public sealed class CliClienteAutocompleteDto
    {
        public int id { get; set; }

        public string strNombreCliente { get; set; } = string.Empty;
    }
}
