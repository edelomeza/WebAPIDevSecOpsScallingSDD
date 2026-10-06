using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using WebAPIDevSecOpsScallingSDD.Services;

namespace SecurityTest.Startup
{
    public class AssemblyIntegrityTests
    {
        private sealed class FakeEnvironment : IHostEnvironment
        {
            public required string EnvironmentName { get; set; }
            public string ApplicationName { get; set; } = "Test";
            public string ContentRootPath { get; set; } = ".";
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }

        private static AssemblyIntegrityCheck CreateCheck(Dictionary<string, string?> config, string environmentName)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(config).Build();
            return new AssemblyIntegrityCheck(configuration, new FakeEnvironment { EnvironmentName = environmentName });
        }

        [Fact]
        public async Task CorrectHashReturnsHealthy()
        {
            var actual = AssemblyIntegrityCheck.ComputeCurrentAssemblySha256();
            var check = CreateCheck(new Dictionary<string, string?> { ["AssemblyIntegrity:ExpectedSha256"] = actual }, "Production");
            var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        [Fact]
        public async Task WrongHashReturnsUnhealthyInProduction()
        {
            var check = CreateCheck(new Dictionary<string, string?> { ["AssemblyIntegrity:ExpectedSha256"] = "DEADBEEF" }, "Production");
            var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task WrongHashReturnsDegradedInDevelopment()
        {
            var check = CreateCheck(new Dictionary<string, string?> { ["AssemblyIntegrity:ExpectedSha256"] = "DEADBEEF" }, "Development");
            var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
            Assert.Equal(HealthStatus.Degraded, result.Status);
        }

        [Fact]
        public async Task MissingHashReturnsDegradedInDevelopmentAndHealthyInProduction()
        {
            var dev = CreateCheck(new Dictionary<string, string?>(), "Development");
            var prod = CreateCheck(new Dictionary<string, string?>(), "Production");
            var devResult = await dev.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
            var prodResult = await prod.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
            Assert.Equal(HealthStatus.Degraded, devResult.Status);
            Assert.Equal(HealthStatus.Healthy, prodResult.Status);
        }
    }
}
