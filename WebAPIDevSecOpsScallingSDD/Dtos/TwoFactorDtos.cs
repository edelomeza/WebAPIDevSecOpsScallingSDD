namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class TwoFactorSetupResponse
    {
        public string Secret { get; set; } = string.Empty;

        public string OtpAuthUri { get; set; } = string.Empty;
    }

    public sealed class TwoFactorVerifyRequest
    {
        public string TotpCode { get; set; } = string.Empty;
    }

    public sealed class TwoFactorVerifyResponse
    {
        public bool Enabled { get; set; }
    }
}
