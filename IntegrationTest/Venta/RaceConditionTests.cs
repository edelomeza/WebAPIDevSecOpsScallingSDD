using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IntegrationTest.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using WebAPIDevSecOpsScallingSDD.Context;

namespace IntegrationTest.Venta
{
    public class RaceConditionTests : IAsyncLifetime
    {
        private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPortBinding(14336, 1433)
            .Build();

        public async Task InitializeAsync() => await _sql.StartAsync().ConfigureAwait(false);

        public async Task DisposeAsync() => await _sql.DisposeAsync().ConfigureAwait(false);

        [Fact]
        public async Task FiveParallelPostsWithStockOneYieldOneCreatedFourConflicts()
        {
            await MigrateAsync();
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            AddAdminRole(client);

            using var stateContent = new StringContent("{\"state\":\"race\"}", Encoding.UTF8, "application/json");
            var state = await client.PostAsync(new Uri("/provider-states", UriKind.Relative), stateContent);
            Assert.Equal(HttpStatusCode.OK, state.StatusCode);

            var tasks = Enumerable.Range(0, 5).Select(_ => PostVentaAsync(client)).ToArray();
            var responses = await Task.WhenAll(tasks);

            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
            Assert.Equal(4, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
            foreach (var response in responses)
            {
                response.Dispose();
            }

            var stock = await GetExistenciaAsync();
            Assert.Equal(0, stock);
        }

        private static async Task<HttpResponseMessage> PostVentaAsync(HttpClient client)
        {
            using var content = new StringContent(
                "{\"idCliCliente\":1,\"idSegUsuario\":1,\"idVenCatEstado\":1,\"strClaveVenta\":\"RACE-0001\",\"Detalles\":[{\"idProProducto\":1,\"intPiezaVenta\":1}]}",
                Encoding.UTF8,
                "application/json");
            return await client.PostAsync(new Uri("/api/v1/ventas", UriKind.Relative), content).ConfigureAwait(false);
        }

        private async Task<int> GetExistenciaAsync()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_sql.GetConnectionString())
                .Options;
            var context = new AppDbContext(options);
            try
            {
                return (await context.ProProductos.AsNoTracking().SingleAsync(p => p.id == 1).ConfigureAwait(false)).intNumeroExistencia;
            }
            finally
            {
                await context.DisposeAsync().ConfigureAwait(false);
            }
        }

        private async Task MigrateAsync()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_sql.GetConnectionString())
                .Options;
            var context = new AppDbContext(options);
            try
            {
                await context.Database.MigrateAsync().ConfigureAwait(false);
            }
            finally
            {
                await context.DisposeAsync().ConfigureAwait(false);
            }
        }

        private WebApplicationFactory<Program> CreateFactory()
        {
#pragma warning disable CA2000 // The inner factory is disposed with the wrapper.
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
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                });
            });
        }

        private static void AddAdminRole(HttpClient client) => client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
    }
}
