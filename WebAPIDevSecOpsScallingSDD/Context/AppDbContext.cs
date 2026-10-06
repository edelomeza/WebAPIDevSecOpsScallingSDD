using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Models;

namespace WebAPIDevSecOpsScallingSDD.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<CliCliente> CliClientes => Set<CliCliente>();
        public DbSet<EmpCatTipoEmpleado> EmpCatTipoEmpleados => Set<EmpCatTipoEmpleado>();
        public DbSet<EmpEmpleado> EmpEmpleados => Set<EmpEmpleado>();
        public DbSet<ProProducto> ProProductos => Set<ProProducto>();
        public DbSet<SegRefreshToken> SegRefreshTokens => Set<SegRefreshToken>();
        public DbSet<SegUsuario> SegUsuarios => Set<SegUsuario>();
        public DbSet<VenCatEstado> VenCatEstados => Set<VenCatEstado>();
        public DbSet<VenPedido> VenPedidos => Set<VenPedido>();
        public DbSet<VenPedidoDetalle> VenPedidoDetalles => Set<VenPedidoDetalle>();
        public DbSet<VenPedidoFactura> VenPedidoFacturas => Set<VenPedidoFactura>();
        public DbSet<VenPedidoPago> VenPedidoPagos => Set<VenPedidoPago>();
        public DbSet<VenVenta> VenVentas => Set<VenVenta>();
        public DbSet<VenVentaDetalle> VenVentaDetalles => Set<VenVentaDetalle>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                .Where(t => typeof(IConcurrenteAuditable).IsAssignableFrom(t.ClrType)))
            {
                modelBuilder.Entity(entityType.ClrType).Property<byte[]>(nameof(IConcurrenteAuditable.RowVersion)).IsRowVersion();
            }

            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            modelBuilder.Entity<VenPedidoPago>().HasIndex(e => e.strIdTransaccion).IsUnique();
            modelBuilder.Entity<VenPedidoFactura>().HasIndex(e => e.strFolioFactura).IsUnique();
        }

        public override int SaveChanges()
        {
            AplicarMitigacionInMemory();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AplicarMitigacionInMemory();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AplicarMitigacionInMemory();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            AplicarMitigacionInMemory();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void AplicarMitigacionInMemory()
        {
            if (!string.Equals(Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
            {
                return;
            }

            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is IConcurrenteAuditable auditable)
                {
                    auditable.RowVersion = new byte[] { 1 };
                }
            }
        }
    }
}
