using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Services;
using ClienteModel = WebAPIDevSecOpsScallingSDD.Models.CliCliente;
using DetalleModel = WebAPIDevSecOpsScallingSDD.Models.VenPedidoDetalle;
using PedidoModel = WebAPIDevSecOpsScallingSDD.Models.VenPedido;
using ProductoModel = WebAPIDevSecOpsScallingSDD.Models.ProProducto;

namespace UnitTest.Consumers
{
    public sealed class FakeEventBus : IEventBus
    {
        private readonly List<object> _published = new();

        public IReadOnlyList<object> Published => _published;

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(message);
            _published.Add(message);
            return Task.CompletedTask;
        }
    }

    public sealed class FakeCacheService : ICacheService
    {
        private readonly Dictionary<string, object?> _store = new();

        public Task<T?> GetAsync<T>(string prefix, string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_store.TryGetValue(prefix + key, out var value) ? (T?)value : default);
        }

        public Task SetAsync<T>(string prefix, string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            _store[prefix + key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string prefix, string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(prefix + key);
            return Task.CompletedTask;
        }
    }

    internal static class ConsumerSeeds
    {
        public static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        public static void SeedBase(AppDbContext context, int existencia = 5, decimal precio = 10m)
        {
            context.CliClientes.Add(new ClienteModel { id = 1, strNombreCliente = "Ana", strCorreoElectronico = "a@test.local", strNumeroTelefono = "5550000001" });
            context.ProProductos.Add(new ProductoModel { id = 1, strNombreProducto = "Tornillo", intNumeroExistencia = existencia, decPrecio = precio });
            context.SaveChanges();
            context.ChangeTracker.Clear();
        }

        public static Guid SeedPedido(AppDbContext context, string estado = "Creado", int cantidad = 2)
        {
            var pedidoId = Guid.NewGuid();
            context.VenPedidos.Add(new PedidoModel { id = pedidoId, idCliCliente = 1, dteFechaPedido = DateTime.UtcNow, decTotal = 20m, strEstadoSaga = estado });
            context.VenPedidoDetalles.Add(new DetalleModel { idVenPedido = pedidoId, idProProducto = 1, intCantidad = cantidad, decPrecioUnitario = 10m });
            context.SaveChanges();
            context.ChangeTracker.Clear();
            return pedidoId;
        }
    }
}
