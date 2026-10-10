namespace WebAPIDevSecOpsScallingSDD.Services
{
    /// <summary>Límites de rate-limit por policy (04-04). Defaults = spec; override por sección RateLimiting + PERF_RATELIMIT_MULTIPLIER.</summary>
    public sealed class RateLimitOptions
    {
        public const string SectionName = "RateLimiting";

        public const string LoginPolicyName = "Login";

        public const string Login2faPolicyName = "Login2faVerify";

        public const string GlobalPolicyName = "Global";

        public const string AdminPolicyName = "Admin";

        public const string ConcurrentWritesPolicyName = "ConcurrentWrites";

        public int LoginPermitLimit { get; set; } = 5;

        public int LoginWindowSeconds { get; set; } = 300;

        public int Login2faPermitLimit { get; set; } = 10;

        public int Login2faWindowSeconds { get; set; } = 300;

        public int GlobalPermitLimit { get; set; } = 1000;

        public int GlobalWindowSeconds { get; set; } = 60;

        public int AdminPermitLimit { get; set; } = 200;

        public int AdminWindowSeconds { get; set; } = 60;

        public int ConcurrentWritesPermitLimit { get; set; } = 10;

        /// <summary>Multiplica todos los límites (relajación solo perf). Valores &lt;1 se tratan como 1; mínimo resultante 1.</summary>
        /// <param name="multiplier">Multiplicador entero.</param>
        public void ApplyMultiplier(int multiplier)
        {
            if (multiplier < 1)
            {
                multiplier = 1;
            }

            LoginPermitLimit = Multiply(LoginPermitLimit, multiplier);
            Login2faPermitLimit = Multiply(Login2faPermitLimit, multiplier);
            GlobalPermitLimit = Multiply(GlobalPermitLimit, multiplier);
            AdminPermitLimit = Multiply(AdminPermitLimit, multiplier);
            ConcurrentWritesPermitLimit = Multiply(ConcurrentWritesPermitLimit, multiplier);
        }

        private static int Multiply(int value, int multiplier)
        {
            var result = (long)value * multiplier;
            if (result < 1)
            {
                return 1;
            }

            return result > int.MaxValue ? int.MaxValue : (int)result;
        }
    }
}
