using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;
using WebAPIDevSecOpsScallingSDD.Context;

namespace IntegrationTest.Common
{
    public class ProviderStatesTests : IAsyncLifetime
    {
        private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPortBinding(14334, 1433)
            .Build();

        public async Task InitializeAsync() => await _sql.StartAsync().ConfigureAwait(false);

        public async Task DisposeAsync() => await _sql.DisposeAsync().ConfigureAwait(false);

        [Fact]
        public async Task PostBaseStateReturnsOkAndSeeds()
        {
            await MigrateAsync();
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            using var content = new StringContent("{\"state\":\"base\"}", Encoding.UTF8, "application/json");

            var response = await client.PostAsync(new Uri("/provider-states", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var context = CreateContext();
            try
            {
                Assert.Equal(1, await context.CliClientes.CountAsync());
            }
            finally
            {
                await context.DisposeAsync();
            }
        }

        [Fact]
        public async Task PostRaceStateResetsStockToOne()
        {
            await MigrateAsync();
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            using var content = new StringContent("{\"state\":\"race\"}", Encoding.UTF8, "application/json");

            var response = await client.PostAsync(new Uri("/provider-states", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var context = CreateContext();
            try
            {
                var producto = await context.ProProductos.SingleAsync(p => p.id == 1);
                Assert.Equal(1, producto.intNumeroExistencia);
            }
            finally
            {
                await context.DisposeAsync();
            }
        }

        [Fact]
        public async Task PostPerfStateSeedsPerfUser()
        {
            await MigrateAsync();
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            using var content = new StringContent("{\"state\":\"perf\"}", Encoding.UTF8, "application/json");

            var response = await client.PostAsync(new Uri("/provider-states", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var context = CreateContext();
            try
            {
                var perf = await context.SegUsuarios.SingleAsync(u => u.id == 2);
                Assert.Equal("perf", perf.strNombre);
            }
            finally
            {
                await context.DisposeAsync();
            }
        }

        [Fact]
        public async Task PostUnknownStateReturnsBadRequest()
        {
            await MigrateAsync();
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            using var content = new StringContent("{\"state\":\"no-existe\"}", Encoding.UTF8, "application/json");

            var response = await client.PostAsync(new Uri("/provider-states", UriKind.Relative), content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private async Task MigrateAsync()
        {
            var context = CreateContext();
            try
            {
                await context.Database.MigrateAsync().ConfigureAwait(false);
            }
            finally
            {
                await context.DisposeAsync().ConfigureAwait(false);
            }
        }

        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_sql.GetConnectionString())
                .Options;
            return new AppDbContext(options);
        }

        private WebApplicationFactory<Program> CreateFactory()
        {
#pragma warning disable CA2000 // The instance is returned to the caller, which disposes it.
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
#pragma warning restore CA2000
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Default"] = _sql.GetConnectionString(),
                        ["UseInMemoryDatabase"] = "false",
                        ["EnableProviderStates"] = "true",
                    });
                });
            });
        }
    }
}
