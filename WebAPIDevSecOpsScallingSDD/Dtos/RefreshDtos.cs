namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class RefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public sealed class RefreshResponse
    {
        public string Token { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;
    }

    public sealed class LogoutRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
