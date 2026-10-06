using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using WebAPIDevSecOpsScallingSDD.Context;

namespace DatabaseTest;

public sealed class SchemaTests : IAsyncLifetime
{
    private const string TableFilter = "'CliCliente','EmpCatTipoEmpleado','EmpEmpleado','ProProducto','SegRefreshToken','SegUsuario','VenCatEstado','VenPedido','VenPedidoDetalle','VenPedidoFactura','VenPedidoPago','VenVenta','VenVentaDetalle'";

    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPortBinding(14335, 1433)
        .Build();

    public async Task InitializeAsync() => await _sql.StartAsync().ConfigureAwait(false);

    public async Task DisposeAsync() => await _sql.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public async Task SchemaMatchesSpecification()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        Assert.Equal(13, await CountAsync(context, "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME IN (" + TableFilter + ")"));
        Assert.Equal(13, await CountAsync(context, "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_TYPE = 'PRIMARY KEY' AND TABLE_NAME IN (" + TableFilter + ")"));
        Assert.Equal(12, await CountAsync(context, "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_TYPE = 'FOREIGN KEY' AND TABLE_NAME IN (" + TableFilter + ")"));
        Assert.Equal(11, await CountAsync(context, "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE DATA_TYPE = 'timestamp' AND TABLE_NAME IN (" + TableFilter + ")"));
        Assert.Equal(6, await CountAsync(context, "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE DATA_TYPE = 'decimal' AND NUMERIC_PRECISION = 18 AND NUMERIC_SCALE = 2 AND TABLE_NAME IN (" + TableFilter + ")"));
        Assert.Equal(2, await CountAsync(context, "SELECT COUNT(*) AS Value FROM sys.indexes i INNER JOIN sys.objects o ON i.object_id = o.object_id WHERE i.is_unique = 1 AND i.is_primary_key = 0 AND o.name IN (" + TableFilter + ")"));
    }

    private static async Task<int> CountAsync(DbContext context, string sql)
    {
        var rows = await context.Database.SqlQueryRaw<int>(sql).ToListAsync().ConfigureAwait(false);
        return rows[0];
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_sql.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }
}
