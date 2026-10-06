namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class Login2FaVerifyRequest
    {
        public string TempToken { get; set; } = string.Empty;

        public string TotpCode { get; set; } = string.Empty;
    }

    public sealed class Login2FaVerifyResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}
