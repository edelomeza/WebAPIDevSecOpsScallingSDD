using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Events;
using WebAPIDevSecOpsScallingSDD.Models;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface IVentasPedidoService
    {
        Task<PedidoResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<PedidoResponseDto> CreateAsync(PedidoCreateDto dto, CancellationToken cancellationToken = default);
    }

    public sealed class VentasPedidoService : IVentasPedidoService
    {
        private const string CachePrefix = "cache:";
        private const string ByIdKeyPrefix = "pedido:";
        private const string VersionKey = "pedido:version";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;
        private readonly IPedidoEventPublisher _publisher;

        public VentasPedidoService(AppDbContext db, ICacheService cache, IPedidoEventPublisher publisher)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(publisher);
            _db = db;
            _cache = cache;
            _publisher = publisher;
        }

        public async Task<PedidoResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync<PedidoResponseDto>(CachePrefix, $"{ByIdKeyPrefix}{id}", cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var entity = await _db.VenPedidos.AsNoTracking().FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return null;
            }

            var detalles = await _db.VenPedidoDetalles.AsNoTracking().Where(d => d.idVenPedido == id).OrderBy(d => d.id).ToListAsync(cancellationToken).ConfigureAwait(false);
            var dto = ToDto(entity, detalles);
            await _cache.SetAsync(CachePrefix, $"{ByIdKeyPrefix}{id}", dto, CacheTtl, cancellationToken).ConfigureAwait(false);
            return dto;
        }

        public async Task<PedidoResponseDto> CreateAsync(PedidoCreateDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            if (dto.Detalles.Count == 0)
            {
                throw new ValidationException("El pedido requiere al menos un detalle.");
            }

            await EnsureExistsAsync(_db.CliClientes.AsNoTracking(), dto.idCliCliente, "Cliente", cancellationToken).ConfigureAwait(false);

            var productIds = dto.Detalles.Select(d => d.idProProducto).Distinct().ToList();
            var productos = await _db.ProProductos.Where(e => productIds.Contains(e.id)).ToDictionaryAsync(e => e.id, cancellationToken).ConfigureAwait(false);
            var total = 0m;
            foreach (var item in dto.Detalles)
            {
                if (!productos.TryGetValue(item.idProProducto, out var producto))
                {
                    throw new ValidationException($"Producto '{item.idProProducto}' no existe.");
                }

                total += producto.decPrecio * item.intCantidad;
            }
            var pedido = new VenPedido
            {
                id = Guid.NewGuid(),
                idCliCliente = dto.idCliCliente,
                dteFechaPedido = DateTime.UtcNow,
                decTotal = total,
                // NOTE (06-04): estado temporal; 06-04 fija la máquina de estados de la saga.
                strEstadoSaga = "Creado",
            };

            // La clave es Guid generado en cliente: pedido + detalles van en un único SaveChanges
            // (atomico en SQL por Tx explícita; en InMemory por SaveChanges).
            var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
                : null;
            try
            {
                _db.VenPedidos.Add(pedido);
                foreach (var item in dto.Detalles)
                {
                    _db.VenPedidoDetalles.Add(new VenPedidoDetalle
                    {
                        idVenPedido = pedido.id,
                        idProProducto = item.idProProducto,
                        intCantidad = item.intCantidad,
                        decPrecioUnitario = productos[item.idProProducto].decPrecio,
                    });
                }

                try
                {
                    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new ConcurrencyConflictException();
                }

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync().ConfigureAwait(false);
                }
            }

            await InvalidateAsync(pedido.id, cancellationToken).ConfigureAwait(false);
            await _publisher.PublishAsync(new PedidoCreadoEvent
            {
                PedidoId = pedido.id,
                ClienteId = pedido.idCliCliente,
                Total = pedido.decTotal,
            }, cancellationToken).ConfigureAwait(false);

            var created = await _db.VenPedidos.AsNoTracking().FirstAsync(e => e.id == pedido.id, cancellationToken).ConfigureAwait(false);
            var createdDetalles = await _db.VenPedidoDetalles.AsNoTracking().Where(d => d.idVenPedido == pedido.id).OrderBy(d => d.id).ToListAsync(cancellationToken).ConfigureAwait(false);
            return ToDto(created, createdDetalles);
        }

        private static async Task EnsureExistsAsync<T>(IQueryable<T> query, int id, string nombre, CancellationToken cancellationToken)
            where T : class
        {
            var exists = await query.AnyAsync(e => EF.Property<int>(e, "id") == id, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new ValidationException($"{nombre} '{id}' no existe.");
            }
        }

        private async Task InvalidateAsync(Guid? id = null, CancellationToken cancellationToken = default)
        {
            if (id.HasValue)
            {
                await _cache.RemoveAsync(CachePrefix, $"{ByIdKeyPrefix}{id.Value}", cancellationToken).ConfigureAwait(false);
            }

            var version = await _cache.GetAsync<int>(CachePrefix, VersionKey, cancellationToken).ConfigureAwait(false);
            await _cache.SetAsync(CachePrefix, VersionKey, version + 1, CacheTtl, cancellationToken).ConfigureAwait(false);
        }

        private static PedidoResponseDto ToDto(VenPedido entity, List<VenPedidoDetalle> detalles)
        {
            return new PedidoResponseDto
            {
                id = entity.id,
                idCliCliente = entity.idCliCliente,
                dteFechaPedido = entity.dteFechaPedido,
                decTotal = entity.decTotal,
                strEstadoSaga = entity.strEstadoSaga,
                Detalles = detalles.Select(d => new PedidoDetalleResponseDto
                {
                    id = d.id,
                    idProProducto = d.idProProducto,
                    intCantidad = d.intCantidad,
                    decPrecioUnitario = d.decPrecioUnitario,
                    RowVersion = d.RowVersion,
                }).ToList(),
                RowVersion = entity.RowVersion,
            };
        }
    }
}
