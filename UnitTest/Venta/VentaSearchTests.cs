using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Services;
using ClienteModel = WebAPIDevSecOpsScallingSDD.Models.CliCliente;
using EstadoModel = WebAPIDevSecOpsScallingSDD.Models.VenCatEstado;
using ProductoModel = WebAPIDevSecOpsScallingSDD.Models.ProProducto;
using UsuarioModel = WebAPIDevSecOpsScallingSDD.Models.SegUsuario;
using VentaModel = WebAPIDevSecOpsScallingSDD.Models.VenVenta;

namespace UnitTest.Venta
{
    public class VentaSearchTests
    {
        [Fact]
        public async Task ClaveExactaConTrimFiltra()
        {
            await using var context = CreateSeededContext();
            var service = new VentaService(context, new FakeCacheService());

            var exacta = await service.SearchAsync("V-00000001", null, null, null, 1, 20);
            var conEspacios = await service.SearchAsync("  V-00000001  ", null, null, null, 1, 20);
            var parcial = await service.SearchAsync("V-0000000", null, null, null, 1, 20);

            Assert.Equal(1, exacta.TotalCount);
            Assert.Equal("V-00000001", exacta.Items[0].strClaveVenta);
            Assert.Equal(1, conEspacios.TotalCount);
            Assert.Equal(0, parcial.TotalCount);
        }

        [Fact]
        public async Task NombreFiltraViaJoinCliente()
        {
            await using var context = CreateSeededContext();
            var service = new VentaService(context, new FakeCacheService());

            var ana = await service.SearchAsync(null, "Ana", null, null, 1, 20);
            var beto = await service.SearchAsync(null, "Beto", null, null, 1, 20);

            Assert.Equal(3, ana.TotalCount);
            Assert.All(ana.Items, item => Assert.Equal(1, item.idCliCliente));
            Assert.Equal(1, beto.TotalCount);
            Assert.Equal(2, beto.Items[0].idCliCliente);
        }

        [Fact]
        public async Task RangoFechasFiltraYExcluyeNulos()
        {
            await using var context = CreateSeededContext();
            var service = new VentaService(context, new FakeCacheService());
            var inicio = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var fin = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

            var febrero = await service.SearchAsync(null, null, inicio, fin, 1, 20);

            Assert.Equal(1, febrero.TotalCount);
            Assert.Equal("V-00000002", febrero.Items[0].strClaveVenta);
        }

        [Fact]
        public async Task CombinacionAndDeCuatroFiltros()
        {
            await using var context = CreateSeededContext();
            var service = new VentaService(context, new FakeCacheService());
            var inicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var fin = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

            var match = await service.SearchAsync("V-00000002", "Ana", inicio, fin, 1, 20);
            var mismatch = await service.SearchAsync("V-00000002", "Beto", inicio, fin, 1, 20);

            Assert.Equal(1, match.TotalCount);
            Assert.Equal(0, mismatch.TotalCount);
        }

        [Fact]
        public async Task SinFiltrosDevuelveTodoPaginado()
        {
            await using var context = CreateSeededContext();
            var service = new VentaService(context, new FakeCacheService());

            var page1 = await service.SearchAsync(null, null, null, null, 1, 3);
            var page2 = await service.SearchAsync(null, null, null, null, 2, 3);

            Assert.Equal(4, page1.TotalCount);
            Assert.Equal(3, page1.Items.Count);
            Assert.Equal(4, Assert.Single(page2.Items).id);
            Assert.Equal(1, page1.Page);
        }

        [Fact]
        public async Task NulosFechaSeIncluyenSinFiltro()
        {
            await using var context = CreateSeededContext();
            var service = new VentaService(context, new FakeCacheService());

            var todo = await service.SearchAsync(null, null, null, null, 1, 20);

            Assert.Equal(4, todo.TotalCount);
            Assert.Contains(todo.Items, item => item.dteFechaHoraCompra is null);
        }

        [Fact]
        public async Task CacheaConLlaveVersionadaYTtl()
        {
            var cache = new FakeCacheService();
            await using var context = CreateSeededContext();
            var service = new VentaService(context, cache);

            var first = await service.SearchAsync("V-00000001", null, null, null, 1, 20);
            var second = await service.SearchAsync("V-00000001", null, null, null, 1, 20);

            Assert.Equal(first.TotalCount, second.TotalCount);
            Assert.Contains(cache.Keys, key => key.StartsWith("cache:venta:search:0:V-00000001::null:null:1:20", StringComparison.Ordinal));
            Assert.All(cache.Ttls, ttl => Assert.Equal(TimeSpan.FromSeconds(60), ttl));
        }

        private static AppDbContext CreateSeededContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(options);
            context.CliClientes.Add(new ClienteModel { id = 1, strNombreCliente = "Ana", strCorreoElectronico = "a@test.local", strNumeroTelefono = "5550000001" });
            context.CliClientes.Add(new ClienteModel { id = 2, strNombreCliente = "Beto", strCorreoElectronico = "b@test.local", strNumeroTelefono = "5550000002" });
            context.SegUsuarios.Add(new UsuarioModel { id = 1, strNombre = "Ana", strPWD = "hash", strCorreoElectronico = "a@test.local" });
            context.VenCatEstados.Add(new EstadoModel { id = 1, strValor = "Vigente" });
            context.ProProductos.Add(new ProductoModel { id = 1, strNombreProducto = "Tornillo", intNumeroExistencia = 9, decPrecio = 10m });
            context.VenVentas.Add(new VentaModel { id = 1, idCliCliente = 1, idSegUsuario = 1, idVenCatEstado = 1, dteFechaHoraCompra = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), strClaveVenta = "V-00000001" });
            context.VenVentas.Add(new VentaModel { id = 2, idCliCliente = 1, idSegUsuario = 1, idVenCatEstado = 1, dteFechaHoraCompra = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), strClaveVenta = "V-00000002" });
            context.VenVentas.Add(new VentaModel { id = 3, idCliCliente = 2, idSegUsuario = 1, idVenCatEstado = 1, dteFechaHoraCompra = null, strClaveVenta = "V-00000003" });
            context.VenVentas.Add(new VentaModel { id = 4, idCliCliente = 1, idSegUsuario = 1, idVenCatEstado = 1, dteFechaHoraCompra = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc), strClaveVenta = "V-00000004" });
            context.SaveChanges();
            context.ChangeTracker.Clear();
            return context;
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
