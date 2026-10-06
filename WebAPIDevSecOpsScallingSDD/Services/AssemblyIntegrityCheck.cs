using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    /// <summary>Verifies the running assembly matches the expected SHA-256 hash.</summary>
    internal sealed class AssemblyIntegrityCheck : IHealthCheck
    {
        private readonly string? _expectedSha256;
        private readonly bool _isDevelopment;

        public AssemblyIntegrityCheck(IConfiguration configuration, IHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(environment);
            _expectedSha256 = configuration["AssemblyIntegrity:ExpectedSha256"];
            _isDevelopment = environment.IsDevelopment();
        }

        /// <summary>Computes the SHA-256 hash of the executing assembly file.</summary>
        /// <returns>Uppercase hex string of the hash.</returns>
        public static string ComputeCurrentAssemblySha256()
        {
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(assemblyPath) || !File.Exists(assemblyPath))
            {
                return string.Empty;
            }

            using var stream = File.OpenRead(assemblyPath);
            return Convert.ToHexString(SHA256.HashData(stream));
        }

        /// <inheritdoc />
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var actual = ComputeCurrentAssemblySha256();
            if (string.IsNullOrEmpty(actual))
            {
                return Task.FromResult(HealthCheckResult.Degraded("Assembly location unavailable"));
            }

            if (string.IsNullOrEmpty(_expectedSha256))
            {
                return Task.FromResult(_isDevelopment
                    ? HealthCheckResult.Degraded("AssemblyIntegrity:ExpectedSha256 not configured")
                    : HealthCheckResult.Healthy("Integrity check bypassed: no expected hash configured"));
            }

            if (string.Equals(actual, _expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(HealthCheckResult.Healthy("Assembly hash matches expected value"));
            }

            return Task.FromResult(_isDevelopment
                ? HealthCheckResult.Degraded($"Assembly hash mismatch: expected {_expectedSha256}, got {actual}")
                : HealthCheckResult.Unhealthy($"Assembly hash mismatch: expected {_expectedSha256}, got {actual}"));
        }
    }
}
