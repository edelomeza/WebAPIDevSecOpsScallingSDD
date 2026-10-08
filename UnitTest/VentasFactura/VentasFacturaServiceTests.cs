using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;
using ClienteModel = WebAPIDevSecOpsScallingSDD.Models.CliCliente;
using FacturaModel = WebAPIDevSecOpsScallingSDD.Models.VenPedidoFactura;
using PedidoModel = WebAPIDevSecOpsScallingSDD.Models.VenPedido;

namespace UnitTest.VentasFactura
{
    public class VentasFacturaServiceTests
    {
        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            using var context = CreateContext();
            var cache = new FakeCacheService();

            Assert.Throws<ArgumentNullException>(() => new VentasFacturaService(null!, cache));
            Assert.Throws<ArgumentNullException>(() => new VentasFacturaService(context, null!));
        }

        [Fact]
        public async Task GetByIdReturnsNullWhenMissingWithoutCaching()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            SeedPedido(context);
            var service = new VentasFacturaService(context, cache);

            Assert.Null(await service.GetByIdAsync(999999));
            Assert.DoesNotContain(cache.Keys, key => key == "cache:factura:999999");
        }

        [Fact]
        public async Task GetByIdPersistsCachesAndKeys()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var pedidoId = SeedPedido(context);
            var facturaId = SeedFactura(context, pedidoId, $"F-TEST-{Guid.NewGuid():N}", "Emitida", 99.99m);
            var service = new VentasFacturaService(context, cache);

            var fetched = await service.GetByIdAsync(facturaId);

            Assert.NotNull(fetched);
            Assert.Equal(facturaId, fetched!.id);
            Assert.Equal(pedidoId, fetched.idVenPedido);
            Assert.Equal(99.99m, fetched.decTotal);
            Assert.Equal("Emitida", fetched.strEstado);
            Assert.StartsWith("F-TEST-", fetched.strFolioFactura, StringComparison.Ordinal);
            Assert.NotNull(fetched.RowVersion);
            Assert.Contains(cache.Keys, key => key == $"cache:factura:{facturaId}");
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
        }

        [Fact]
        public async Task GetByIdServesCacheWithoutHittingDb()
        {
            var cache = new FakeCacheService();
            await using var context = CreateContext();
            var pedidoId = SeedPedido(context);
            var facturaId = SeedFactura(context, pedidoId, $"F-TEST-{Guid.NewGuid():N}", "Emitida", 10m);
            var service = new VentasFacturaService(context, cache);

            var first = await service.GetByIdAsync(facturaId);
            Assert.NotNull(first);

            context.VenPedidoFacturas.RemoveRange(context.VenPedidoFacturas);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            Assert.Equal(0, await context.VenPedidoFacturas.CountAsync());

            var stale = await service.GetByIdAsync(facturaId);
            Assert.NotNull(stale);
            Assert.Equal(facturaId, stale!.id);
            Assert.Equal(first!.strFolioFactura, stale.strFolioFactura);
            Assert.Equal(10m, stale.decTotal);
        }

        [Fact]
        public async Task GetByIdMapsRfcAndFechaEmision()
        {
            await using var context = CreateContext();
            var pedidoId = SeedPedido(context);
            var facturaId = SeedFactura(context, pedidoId, $"F-TEST-{Guid.NewGuid():N}", "Emitida", 50m, rfc: "GODE561231GR8");
            var service = new VentasFacturaService(context, new FakeCacheService());

            var fetched = await service.GetByIdAsync(facturaId);

            Assert.NotNull(fetched);
            Assert.Equal("GODE561231GR8", fetched!.strRFC);
            Assert.NotEqual(default, fetched.dteFechaEmision);
        }

        private static int SeedFactura(AppDbContext context, Guid pedidoId, string folio, string estado, decimal total, string? rfc = null)
        {
            var entity = new FacturaModel
            {
                idVenPedido = pedidoId,
                strFolioFactura = folio,
                strRFC = rfc,
                decTotal = total,
                dteFechaEmision = DateTime.UtcNow,
                strEstado = estado,
            };
            context.VenPedidoFacturas.Add(entity);
            context.SaveChanges();
            context.ChangeTracker.Clear();
            return entity.id;
        }

        private static Guid SeedPedido(AppDbContext context)
        {
            SeedBase(context);
            var pedidoId = Guid.NewGuid();
            context.VenPedidos.Add(new PedidoModel { id = pedidoId, idCliCliente = 1, dteFechaPedido = DateTime.UtcNow, decTotal = 10m, strEstadoSaga = "Creado" });
            context.SaveChanges();
            context.ChangeTracker.Clear();
            return pedidoId;
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
