using System.Collections.Generic;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebAPIDevSecOpsScallingSDD;

namespace UnitTest.DI
{
    public class ServiceRegistrationTests
    {
        private static ServiceCollection CreateServices(Dictionary<string, string?>? configData = null)
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(configData ?? new Dictionary<string, string?>())
                .Build();
            WebApiServiceCollectionExtensions.AddWebApiDevSecOpsServices(services, config);
            return services;
        }

        [Fact]
        public void CorsOptionsAreRegistered()
        {
            var services = CreateServices();
            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IOptions<CorsOptions>>());
        }

        [Fact]
        public void CorsPolicyIsLockedDownByDefault()
        {
            var services = CreateServices();
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
            var policy = options.GetPolicy(options.DefaultPolicyName ?? "");
            Assert.NotNull(policy);
            Assert.Empty(policy.Origins ?? []);
            Assert.False(policy.AllowAnyOrigin);
        }

        [Fact]
        public void CorsPolicyUsesConfiguredOrigins()
        {
            var services = CreateServices(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://yourproductiondomain.com",
            });
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
            var policy = options.GetPolicy(options.DefaultPolicyName ?? "");
            Assert.NotNull(policy);
            Assert.Contains("https://yourproductiondomain.com", policy.Origins!);
        }
    }
}
