namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class LoginRequest
    {
        public string strNombre { get; set; } = string.Empty;

        public string strPasswordPlano { get; set; } = string.Empty;
    }

    public sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public bool Requires2fa { get; set; }

        public string? TempToken { get; set; }
    }
}
