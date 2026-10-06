using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.MsSql;
using WebAPIDevSecOpsScallingSDD.Context;

namespace DatabaseTest;

public sealed class MigrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPortBinding(14333, 1433)
        .Build();

    public async Task InitializeAsync() => await _sql.StartAsync().ConfigureAwait(false);

    public async Task DisposeAsync() => await _sql.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public async Task MigrationsSeedAndRollbackFlow()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME <> '__EFMigrationsHistory'")
            .ToListAsync();
        var expected = new[]
        {
            "CliCliente", "EmpCatTipoEmpleado", "EmpEmpleado", "ProProducto", "SegRefreshToken",
            "SegUsuario", "VenCatEstado", "VenPedido", "VenPedidoDetalle", "VenPedidoFactura",
            "VenPedidoPago", "VenVenta", "VenVentaDetalle",
        };
        Assert.All(expected, table => Assert.Contains(table, tables));

        await DatabaseSeeder.SeedAsync(context);
        await DatabaseSeeder.SeedAsync(context);

        Assert.Equal(1, await context.CliClientes.CountAsync());
        Assert.Equal(1, await context.VenCatEstados.CountAsync());
        Assert.Equal(1, await context.EmpCatTipoEmpleados.CountAsync());
        Assert.Equal(1, await context.SegUsuarios.CountAsync());
        Assert.Equal(1, await context.ProProductos.CountAsync());
        Assert.Equal(1, await context.EmpEmpleados.CountAsync());
        Assert.Equal(1, await context.VenVentas.CountAsync());
        Assert.Equal(1, await context.VenVentaDetalles.CountAsync());
        Assert.Equal(1, await context.VenPedidos.CountAsync());
        Assert.Equal(1, await context.VenPedidoDetalles.CountAsync());
        Assert.Equal(1, await context.VenPedidoPagos.CountAsync());
        Assert.Equal(1, await context.VenPedidoFacturas.CountAsync());
        Assert.Equal(1, await context.SegRefreshTokens.CountAsync());

        await context.GetService<IMigrator>().MigrateAsync("0");
        var applied = await context.Database
            .SqlQueryRaw<string>("SELECT MigrationId AS Value FROM [__EFMigrationsHistory]")
            .ToListAsync();
        Assert.Empty(applied);
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_sql.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }
}
