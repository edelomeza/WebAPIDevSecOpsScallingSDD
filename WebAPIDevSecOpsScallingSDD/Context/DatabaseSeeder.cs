using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WebAPIDevSecOpsScallingSDD.Context
{
    internal static class DatabaseSeeder
    {
        private static readonly Guid SeedPedidoId = new("11111111-1111-1111-1111-111111111111");
        private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static async Task<bool> ApplyStateAsync(AppDbContext context, string state, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentException.ThrowIfNullOrWhiteSpace(state);
            await SeedAsync(context, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(context.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(state, "base", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "saga", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(state, "race", StringComparison.OrdinalIgnoreCase))
            {
                await ResetRaceStockAsync(context, cancellationToken).ConfigureAwait(false);
                return true;
            }

            if (string.Equals(state, "perf", StringComparison.OrdinalIgnoreCase))
            {
                await SeedPerfUsuarioAsync(context, cancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (!string.Equals(context.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
            {
                return;
            }

            var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await SeedCatalogsAsync(context, cancellationToken).ConfigureAwait(false);
                await SeedSegUsuarioAsync(context, cancellationToken).ConfigureAwait(false);
                await SeedCliClienteAsync(context, cancellationToken).ConfigureAwait(false);
                await SeedProProductoAsync(context, cancellationToken).ConfigureAwait(false);
                await SeedEmpEmpleadoAsync(context, cancellationToken).ConfigureAwait(false);
                await SeedVenVentaAsync(context, cancellationToken).ConfigureAwait(false);
                await SeedVenPedidoAsync(context, cancellationToken).ConfigureAwait(false);
                await SeedSegRefreshTokenAsync(context, cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }

        private static async Task SeedCatalogsAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            if (!await context.VenCatEstados.AnyAsync(e => e.id == 1, cancellationToken).ConfigureAwait(false))
            {
                await context.Database.ExecuteSqlAsync(
                    $"""
                    SET IDENTITY_INSERT [VenCatEstado] ON;
                    INSERT INTO [VenCatEstado] ([id], [strValor], [strDescripcion]) VALUES ({1}, {"Vigente"}, {"Venta vigente"});
                    SET IDENTITY_INSERT [VenCatEstado] OFF;
                    """, cancellationToken).ConfigureAwait(false);
            }

            if (!await context.EmpCatTipoEmpleados.AnyAsync(e => e.id == 1, cancellationToken).ConfigureAwait(false))
            {
                await context.Database.ExecuteSqlAsync(
                    $"""
                    SET IDENTITY_INSERT [EmpCatTipoEmpleado] ON;
                    INSERT INTO [EmpCatTipoEmpleado] ([id], [strValor], [strDescripcion]) VALUES ({1}, {"Cajero"}, {"Empleado de caja"});
                    SET IDENTITY_INSERT [EmpCatTipoEmpleado] OFF;
                    """, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task SeedSegUsuarioAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            if (await context.SegUsuarios.AnyAsync(e => e.id == 1, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await context.Database.ExecuteSqlAsync(
                $"""
                SET IDENTITY_INSERT [SegUsuario] ON;
                INSERT INTO [SegUsuario] ([id], [strNombre], [strPWD], [strCorreoElectronico], [dteFechaRegistro], [bln2FAHabilitado], [str2FASecreto])
                VALUES ({1}, {"seed"}, {"SEED_PLACEHOLDER_HASH_NOT_REAL"}, {"seed@test.local"}, {SeedDate}, {false}, {null});
                SET IDENTITY_INSERT [SegUsuario] OFF;
                """, cancellationToken).ConfigureAwait(false);
        }

        private static async Task SeedCliClienteAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            if (await context.CliClientes.AnyAsync(e => e.id == 1, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await context.Database.ExecuteSqlAsync(
                $"""
                SET IDENTITY_INSERT [CliCliente] ON;
                INSERT INTO [CliCliente] ([id], [strNombreCliente], [strDireccionCliente], [strCorreoElectronico], [strNumeroTelefono], [strCreadoPorUsuario])
                VALUES ({1}, {"Cliente Seed"}, {"Calle Seed 1"}, {"cliente.seed@test.local"}, {"5550000001"}, {"seed"});
                SET IDENTITY_INSERT [CliCliente] OFF;
                """, cancellationToken).ConfigureAwait(false);
        }

        private static async Task SeedProProductoAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            if (await context.ProProductos.AnyAsync(e => e.id == 1, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await context.Database.ExecuteSqlAsync(
                $"""
                SET IDENTITY_INSERT [ProProducto] ON;
                INSERT INTO [ProProducto] ([id], [strNombreProducto], [strURLImagen], [strDescripcion], [intNumeroExistencia], [decPrecio], [strCreadoPorUsuario])
                VALUES ({1}, {"Producto Seed"}, {null}, {"Producto minimo de seed"}, {1}, {99.99m}, {"seed"});
                SET IDENTITY_INSERT [ProProducto] OFF;
                """, cancellationToken).ConfigureAwait(false);
        }

        private static async Task SeedEmpEmpleadoAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            if (await context.EmpEmpleados.AnyAsync(e => e.id == 1, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await context.Database.ExecuteSqlAsync(
                $"""
                SET IDENTITY_INSERT [EmpEmpleado] ON;
                INSERT INTO [EmpEmpleado] ([id], [strNombre], [strAPaterno], [strAMaterno], [strCURP], [idEmpCatTipoEmpleado])
                VALUES ({1}, {"Empleado"}, {"Seed"}, {null}, {null}, {1});
                SET IDENTITY_INSERT [EmpEmpleado] OFF;
                """, cancellationToken).ConfigureAwait(false);
        }

        private static async Task SeedVenVentaAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            if (await context.VenVentas.AnyAsync(e => e.id == 1, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await context.Database.ExecuteSqlAsync(
                $"""
                SET IDENTITY_INSERT [VenVenta] ON;
                INSERT INTO [VenVenta] ([id], [idCliCliente], [idSegUsuario], [idVenCatEstado], [dteFechaHoraCompra], [strClaveVenta])
                VALUES ({1}, {1}, {1}, {1}, {SeedDate}, {"SEED-0001"});
                SET IDENTITY_INSERT [VenVenta] OFF;
                """, cancellationToken).ConfigureAwait(false);

            if (!await context.VenVentaDetalles.AnyAsync(e => e.id == 1, cancellationToken).ConfigureAwait(false))
            {
                await context.Database.ExecuteSqlAsync(
                    $"""
                    SET IDENTITY_INSERT [VenVentaDetalle] ON;
                    INSERT INTO [VenVentaDetalle] ([id], [idVenVenta], [idProProducto], [intPiezaVenta], [decTotalVenta])
                    VALUES ({1}, {1}, {1}, {1}, {99.99m});
                    SET IDENTITY_INSERT [VenVentaDetalle] OFF;
                    """, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task SeedVenPedidoAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            if (!await context.VenPedidos.AnyAsync(e => e.id == SeedPedidoId, cancellationToken).ConfigureAwait(false))
            {
                await context.Database.ExecuteSqlAsync(
                    $"""
                    INSERT INTO [VenPedido] ([id], [idCliCliente], [dteFechaPedido], [decTotal], [strEstadoSaga], [strMotivoRechazo], [LegacyVentaId])
                    VALUES ({SeedPedidoId}, {1}, {SeedDate}, {99.99m}, {"Registrado"}, {null}, {null});
                    """, cancellationToken).ConfigureAwait(false);
            }

            if (!await context.VenPedidoDetalles.AnyAsync(e => e.id == 1, cancellationToken).ConfigureAwait(false))
            {
                await context.Database.ExecuteSqlAsync(
                    $"""
                    SET IDENTITY_INSERT [VenPedidoDetalle] ON;
                    INSERT INTO [VenPedidoDetalle] ([id], [idVenPedido], [idProProducto], [intCantidad], [decPrecioUnitario])
                    VALUES ({1}, {SeedPedidoId}, {1}, {1}, {99.99m});
                    SET IDENTITY_INSERT [VenPedidoDetalle] OFF;
                    """, cancellationToken).ConfigureAwait(false);
            }

            if (!await context.VenPedidoPagos.AnyAsync(e => e.id == 1, cancellationToken).ConfigureAwait(false))
            {
                await context.Database.ExecuteSqlAsync(
                    $"""
                    SET IDENTITY_INSERT [VenPedidoPago] ON;
                    INSERT INTO [VenPedidoPago] ([id], [idVenPedido], [decMonto], [strMetodoPago], [strIdTransaccion], [strEstado], [dteFechaPago])
                    VALUES ({1}, {SeedPedidoId}, {99.99m}, {"Efectivo"}, {null}, {"Aprobado"}, {SeedDate});
                    SET IDENTITY_INSERT [VenPedidoPago] OFF;
                    """, cancellationToken).ConfigureAwait(false);
            }

            if (!await context.VenPedidoFacturas.AnyAsync(e => e.id == 1, cancellationToken).ConfigureAwait(false))
            {
                await context.Database.ExecuteSqlAsync(
                    $"""
                    SET IDENTITY_INSERT [VenPedidoFactura] ON;
                    INSERT INTO [VenPedidoFactura] ([id], [idVenPedido], [strFolioFactura], [strRFC], [decTotal], [dteFechaEmision], [strEstado])
                    VALUES ({1}, {SeedPedidoId}, {"F-SEED-1"}, {null}, {99.99m}, {SeedDate}, {"Emitida"});
                    SET IDENTITY_INSERT [VenPedidoFactura] OFF;
                    """, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task SeedSegRefreshTokenAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            if (await context.SegRefreshTokens.AnyAsync(e => e.id == 1, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await context.Database.ExecuteSqlAsync(
                $"""
                SET IDENTITY_INSERT [SegRefreshToken] ON;
                INSERT INTO [SegRefreshToken] ([id], [idSegUsuario], [strTokenHash], [dteExpiresAt], [dteCreatedAt], [dteRevokedAt], [strReplacedByTokenHash])
                VALUES ({1}, {1}, {"SEED_TOKEN_HASH_NOT_REAL"}, {SeedDate.AddDays(30)}, {SeedDate}, {null}, {null});
                SET IDENTITY_INSERT [SegRefreshToken] OFF;
                """, cancellationToken).ConfigureAwait(false);
        }

        private static async Task ResetRaceStockAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await context.Database.ExecuteSqlAsync(
                    $"""
                    UPDATE [ProProducto] SET [intNumeroExistencia] = {1} WHERE [id] = {1};
                    """, cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }

        private static async Task SeedPerfUsuarioAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            if (await context.SegUsuarios.AnyAsync(e => e.id == 2, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await context.Database.ExecuteSqlAsync(
                    $"""
                    SET IDENTITY_INSERT [SegUsuario] ON;
                    INSERT INTO [SegUsuario] ([id], [strNombre], [strPWD], [strCorreoElectronico], [dteFechaRegistro], [bln2FAHabilitado], [str2FASecreto])
                    VALUES ({2}, {"perf"}, {"PERF_PLACEHOLDER_HASH_NOT_REAL"}, {"perf@test.local"}, {SeedDate}, {false}, {null});
                    SET IDENTITY_INSERT [SegUsuario] OFF;
                    """, cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
