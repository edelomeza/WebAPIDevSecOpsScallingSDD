using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;
using ClienteModel = WebAPIDevSecOpsScallingSDD.Models.CliCliente;
using FacturaModel = WebAPIDevSecOpsScallingSDD.Models.VenPedidoFactura;
using PagoModel = WebAPIDevSecOpsScallingSDD.Models.VenPedidoPago;
using PedidoModel = WebAPIDevSecOpsScallingSDD.Models.VenPedido;

namespace UnitTest.VentasDashboard
{
    public class VentasDashboardServiceTests
    {
        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            using var context = CreateContext();
            var cache = new FakeCacheService();

            Assert.Throws<ArgumentNullException>(() => new VentasDashboardService(null!, cache));
            Assert.Throws<ArgumentNullException>(() => new VentasDashboardService(context, null!));
        }

        [Fact]
        public async Task NullFilterReturnsZerosAndCaches()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context);
            var service = new VentasDashboardService(context, cache);

            var dto = await service.GetAsync(null);

            Assert.Equal(0, dto.TotalPedidos);
            Assert.Equal(0, dto.TotalPagos);
            Assert.Equal(0, dto.TotalFacturas);
            Assert.Equal(0m, dto.MontoTotalPedidos);
            Assert.Equal(0m, dto.MontoTotalPagos);
            Assert.Equal(0m, dto.MontoTotalFacturas);
            Assert.Empty(dto.PorEstadoSaga);
            Assert.Equal(0, dto.ProfundidadCola);
            Assert.Contains(cache.Keys, key => key == "cache:dashboard:null:null:null");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
        }

        [Fact]
        public async Task CountsSumsGroupsAndOrders()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context);
            var first = SeedPedido(context, "Creado", 10m, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
            SeedPedido(context, "Creado", 20m, new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc));
            SeedPedido(context, "Procesado", 5m, new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc));
            SeedPago(context, first, 7m, new DateTime(2026, 3, 1, 1, 0, 0, DateTimeKind.Utc));
            SeedPago(context, first, 3m, new DateTime(2026, 3, 2, 1, 0, 0, DateTimeKind.Utc));
            SeedFactura(context, first, 99.99m, new DateTime(2026, 3, 1, 2, 0, 0, DateTimeKind.Utc));
            var service = new VentasDashboardService(context, cache);

            var dto = await service.GetAsync(new DashboardFilterDto());

            Assert.Equal(3, dto.TotalPedidos);
            Assert.Equal(2, dto.TotalPagos);
            Assert.Equal(1, dto.TotalFacturas);
            Assert.Equal(35m, dto.MontoTotalPedidos);
            Assert.Equal(10m, dto.MontoTotalPagos);
            Assert.Equal(99.99m, dto.MontoTotalFacturas);
            Assert.Equal(2, dto.PorEstadoSaga.Count);
            Assert.Equal("Creado", dto.PorEstadoSaga[0].Estado);
            Assert.Equal(2, dto.PorEstadoSaga[0].Total);
            Assert.Equal("Procesado", dto.PorEstadoSaga[1].Estado);
            Assert.Equal(1, dto.PorEstadoSaga[1].Total);
            Assert.Equal(0, dto.ProfundidadCola);
        }

        [Fact]
        public async Task DateFilterIsInclusiveOnBoundaries()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context);
            var pivot = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);
            var pedidoId = SeedPedido(context, "Creado", 42m, pivot);
            SeedPago(context, pedidoId, 7m, pivot);
            SeedFactura(context, pedidoId, 99.99m, pivot);
            var service = new VentasDashboardService(context, cache);

            var exact = await service.GetAsync(new DashboardFilterDto { Desde = pivot, Hasta = pivot });
            Assert.Equal(1, exact.TotalPedidos);
            Assert.Equal(42m, exact.MontoTotalPedidos);
            Assert.Equal(1, exact.TotalPagos);
            Assert.Equal(7m, exact.MontoTotalPagos);
            Assert.Equal(1, exact.TotalFacturas);
            Assert.Equal(99.99m, exact.MontoTotalFacturas);

            var after = await service.GetAsync(new DashboardFilterDto { Desde = pivot.AddTicks(1) });
            Assert.Equal(0, after.TotalPedidos);
            Assert.Equal(0m, after.MontoTotalPedidos);
            Assert.Equal(0, after.TotalPagos);
            Assert.Equal(0, after.TotalFacturas);

            var before = await service.GetAsync(new DashboardFilterDto { Hasta = pivot.AddTicks(-1) });
            Assert.Equal(0, before.TotalPedidos);
            Assert.Equal(0, before.TotalPagos);
            Assert.Equal(0, before.TotalFacturas);
        }

        [Fact]
        public async Task EstadoFilterTrimsAndMatches()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context);
            SeedPedido(context, "Creado", 10m, DateTime.UtcNow);
            SeedPedido(context, "Procesado", 10m, DateTime.UtcNow);
            var service = new VentasDashboardService(context, cache);

            var dto = await service.GetAsync(new DashboardFilterDto { EstadoSaga = "  Creado  " });

            Assert.Equal(1, dto.TotalPedidos);
            Assert.Single(dto.PorEstadoSaga);
            Assert.Equal("Creado", dto.PorEstadoSaga[0].Estado);
            Assert.Equal("cache:dashboard:null:null:Creado", Assert.Single(cache.Keys));
        }

        [Fact]
        public async Task ServesCacheWithoutHittingDb()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context);
            SeedPedido(context, "Creado", 15m, DateTime.UtcNow);
            var service = new VentasDashboardService(context, cache);

            var first = await service.GetAsync(new DashboardFilterDto());
            Assert.Equal(1, first.TotalPedidos);

            context.VenPedidoPagos.RemoveRange(context.VenPedidoPagos);
            context.VenPedidoFacturas.RemoveRange(context.VenPedidoFacturas);
            context.VenPedidoDetalles.RemoveRange(context.VenPedidoDetalles);
            context.VenPedidos.RemoveRange(context.VenPedidos);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            Assert.Equal(0, await context.VenPedidos.CountAsync());

            var stale = await service.GetAsync(new DashboardFilterDto());
            Assert.Equal(1, stale.TotalPedidos);
            Assert.Equal(15m, stale.MontoTotalPedidos);
        }

        [Fact]
        public async Task CacheKeySanitizesForbiddenPatterns()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedBase(context);
            var service = new VentasDashboardService(context, cache);

            var dto = await service.GetAsync(new DashboardFilterDto { EstadoSaga = "ToKeN-SeCrEt-PaSsWoRd:x" });

            Assert.Equal(0, dto.TotalPedidos);
            Assert.Single(cache.Keys);
            Assert.Equal("cache:dashboard:null:null:_-_-_-x", cache.Keys[0]);
            Assert.DoesNotContain("password", cache.Keys[0], StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", cache.Keys[0], StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", cache.Keys[0], StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ResponseExposesNoSensitiveData()
        {
            await using var context = CreateContext();
            SeedBase(context);
            var pedidoId = SeedPedido(context, "Creado", 10m, DateTime.UtcNow);
            SeedFactura(context, pedidoId, 10m, DateTime.UtcNow, rfc: "GODE561231GR8");
            var service = new VentasDashboardService(context, new FakeCacheService());

            var dto = await service.GetAsync(new DashboardFilterDto());
            var json = JsonSerializer.Serialize(dto);

            Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("rfc", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("folio", json, StringComparison.OrdinalIgnoreCase);
        }

        private static Guid SeedPedido(AppDbContext context, string estado, decimal total, DateTime fecha)
        {
            var pedidoId = Guid.NewGuid();
            context.VenPedidos.Add(new PedidoModel
            {
                id = pedidoId,
                idCliCliente = 1,
                dteFechaPedido = fecha,
                decTotal = total,
                strEstadoSaga = estado,
            });
            context.SaveChanges();
            context.ChangeTracker.Clear();
            return pedidoId;
        }

        private static void SeedPago(AppDbContext context, Guid pedidoId, decimal monto, DateTime fecha)
        {
            context.VenPedidoPagos.Add(new PagoModel
            {
                idVenPedido = pedidoId,
                decMonto = monto,
                strEstado = "Procesado",
                dteFechaPago = fecha,
            });
            context.SaveChanges();
            context.ChangeTracker.Clear();
        }

        private static void SeedFactura(AppDbContext context, Guid pedidoId, decimal total, DateTime fecha, string? rfc = null)
        {
            context.VenPedidoFacturas.Add(new FacturaModel
            {
                idVenPedido = pedidoId,
                strFolioFactura = $"F-TEST-{Guid.NewGuid():N}",
                strRFC = rfc,
                decTotal = total,
                dteFechaEmision = fecha,
                strEstado = "Emitida",
            });
            context.SaveChanges();
            context.ChangeTracker.Clear();
        }

        private static void SeedBase(AppDbContext context)
        {
            if (!context.CliClientes.Any())
            {
                context.CliClientes.Add(new ClienteModel { id = 1, strNombreCliente = "Ana", strCorreoElectronico = "a@test.local", strNumeroTelefono = "5550000001" });
                context.SaveChanges();
                context.ChangeTracker.Clear();
            }
        }

        private static AppDbContext CreateContext()
        {
            return new AppDbContext(CreateOptions());
        }

        private static DbContextOptions<AppDbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        private sealed class FakeCacheService : ICacheService
        {
            private readonly Dictionary<string, object?> _store = new();

            public List<string> Keys => new(_store.Keys);

            public readonly List<TimeSpan> Ttls = new();

            public Task<T?> GetAsync<T>(string prefix, string key, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_store.TryGetValue(prefix + key, out var value) ? (T?)value : default);
            }

            public Task SetAsync<T>(string prefix, string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
            {
                _store[prefix + key] = value;
                Ttls.Add(ttl);
                return Task.CompletedTask;
            }

            public Task RemoveAsync(string prefix, string key, CancellationToken cancellationToken = default)
            {
                _store.Remove(prefix + key);
                return Task.CompletedTask;
            }
        }
    }
}
