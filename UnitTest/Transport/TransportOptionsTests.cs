using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAPIDevSecOpsScallingSDD;

namespace UnitTest.Transport
{
    public class TransportOptionsTests
    {
        [Fact]
        public void SqsWithoutRegionThrowsAtStartup()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Transport"] = "SQS" })
                .Build();

            var ex = Assert.Throws<InvalidOperationException>(() => WebApiServiceCollectionExtensions.AddWebApiDevSecOpsServices(services, configuration));
            Assert.Contains("Sqs:Region", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void DefaultTransportRegistersInMemoryWithoutThrowing()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            WebApiServiceCollectionExtensions.AddWebApiDevSecOpsServices(services, configuration);

            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider);
        }

        [Fact]
        public void SqsWithRegionRegistersWithoutThrowing()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Transport"] = "SQS", ["Sqs:Region"] = "us-east-1" })
                .Build();

            WebApiServiceCollectionExtensions.AddWebApiDevSecOpsServices(services, configuration);

            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider);
        }
    }
}
