namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class EmpEmpleadoDto
    {
        public int id { get; set; }

        public string strNombre { get; set; } = string.Empty;

        public string? strAPaterno { get; set; }

        public string? strAMaterno { get; set; }

        public string? strCURP { get; set; }

        public int? idEmpCatTipoEmpleado { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }

    public sealed class EmpEmpleadoCreateDto
    {
        public string strNombre { get; set; } = string.Empty;

        public string? strAPaterno { get; set; }

        public string? strAMaterno { get; set; }

        public string? strCURP { get; set; }

        public int? idEmpCatTipoEmpleado { get; set; }
    }

    public sealed class EmpEmpleadoUpdateDto
    {
        public required int id { get; set; }

        public string strNombre { get; set; } = string.Empty;

        public string? strAPaterno { get; set; }

        public string? strAMaterno { get; set; }

        public string? strCURP { get; set; }

        public int? idEmpCatTipoEmpleado { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }

    public sealed class EmpEmpleadoDeleteDto
    {
        public required int id { get; set; }

        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }
}
