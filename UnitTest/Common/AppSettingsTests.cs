using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UnitTest.Common
{
    public class AppSettingsTests
    {
        private static readonly string RepoRoot = FindRepoRoot();

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WebAPIDevSecOpsScallingSDD.slnx")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }

        private static string ReadExampleJson() => File.ReadAllText(Path.Combine(RepoRoot, "WebAPIDevSecOpsScallingSDD", "appsettings.Example.json"));

        private static string ReadAppSettingsJson() => File.ReadAllText(Path.Combine(RepoRoot, "WebAPIDevSecOpsScallingSDD", "appsettings.json"));

        [Fact]
        public void ExampleJsonShouldBeValidAndHaveNoSecrets()
        {
            var raw = ReadExampleJson();
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            Assert.True(root.GetProperty("Jwt").TryGetProperty("Key", out var jwtKey));
            Assert.Contains("PLACEHOLDER", jwtKey.GetString(), StringComparison.OrdinalIgnoreCase);

            Assert.True(root.TryGetProperty("Kestrel", out var kestrel));
            Assert.True(kestrel.GetProperty("Limits").TryGetProperty("MaxRequestBodySize", out _));
            Assert.True(root.TryGetProperty("Cors", out var cors));
            Assert.True(cors.GetProperty("AllowedOrigins").GetArrayLength() > 0);
            Assert.True(root.TryGetProperty("AssemblyIntegrity", out var integrity));
            Assert.True(integrity.TryGetProperty("ExpectedSha256", out _));
            Assert.True(root.TryGetProperty("ConnectionStrings", out var cs));
            Assert.True(cs.TryGetProperty("Default", out _));
            Assert.True(root.TryGetProperty("UseInMemoryDatabase", out _));
            Assert.True(root.TryGetProperty("SkipMigration", out _));
            Assert.True(root.TryGetProperty("EnableProviderStates", out _));
            Assert.True(root.TryGetProperty("Redis", out var redis));
            Assert.True(redis.TryGetProperty("ConnectionString", out _));
            Assert.True(root.TryGetProperty("RateLimiting", out var rateLimiting));
            Assert.True(rateLimiting.TryGetProperty("LoginPermitLimit", out _));
            Assert.True(rateLimiting.TryGetProperty("LoginWindowSeconds", out _));
            Assert.True(rateLimiting.TryGetProperty("Login2faPermitLimit", out _));
            Assert.True(rateLimiting.TryGetProperty("Login2faWindowSeconds", out _));
            Assert.True(rateLimiting.TryGetProperty("GlobalPermitLimit", out _));
            Assert.True(rateLimiting.TryGetProperty("GlobalWindowSeconds", out _));
            Assert.True(rateLimiting.TryGetProperty("AdminPermitLimit", out _));
            Assert.True(rateLimiting.TryGetProperty("AdminWindowSeconds", out _));
            Assert.True(rateLimiting.TryGetProperty("ConcurrentWritesPermitLimit", out _));
        }

        [Fact]
        public void AppSettingsRealShouldNotContainSensitivePatterns()
        {
            var raw = ReadAppSettingsJson();
            Assert.DoesNotMatch(new Regex("(?i)password\\s*[:=]\\s*\\S+", RegexOptions.Compiled), raw);
            Assert.DoesNotMatch(new Regex("[0-9a-fA-F]{64}", RegexOptions.Compiled), raw);
            Assert.DoesNotMatch(new Regex("-----BEGIN", RegexOptions.Compiled), raw);
        }

        [Fact]
        public void ExampleJsonShouldNotContainFutureKeys()
        {
            var raw = ReadExampleJson();
            using var doc = JsonDocument.Parse(raw);

            Assert.DoesNotContain("PERF_", raw, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"Transport\"", raw, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("StackName", raw, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DB_USER", raw, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DB_PASSWORD", raw, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PORT", raw, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Observability", raw, StringComparison.OrdinalIgnoreCase);
        }
    }
}
