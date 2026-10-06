using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Models;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface IVentaService
    {
        Task<VenVentaDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<VenVentaDto> CreateAsync(VenVentaCreateDto dto, CancellationToken cancellationToken = default);
    }

    public sealed class VentaService : IVentaService
    {
        private const string CachePrefix = "cache:";
        private const string ByIdKeyPrefix = "venta:";
        private const string VersionKey = "venta:version";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;

        public VentaService(AppDbContext db, ICacheService cache)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            _db = db;
            _cache = cache;
        }

        public async Task<VenVentaDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync<VenVentaDto>(CachePrefix, $"{ByIdKeyPrefix}{id}", cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var entity = await _db.VenVentas.AsNoTracking().FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return null;
            }

            var detalles = await _db.VenVentaDetalles.AsNoTracking().Where(d => d.idVenVenta == id).OrderBy(d => d.id).ToListAsync(cancellationToken).ConfigureAwait(false);
            var dto = ToDto(entity, detalles);
            await _cache.SetAsync(CachePrefix, $"{ByIdKeyPrefix}{id}", dto, CacheTtl, cancellationToken).ConfigureAwait(false);
            return dto;
        }

        public async Task<VenVentaDto> CreateAsync(VenVentaCreateDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            if (dto.Detalles.Count == 0)
            {
                throw new ValidationException("La venta requiere al menos un detalle.");
            }

            await EnsureExistsAsync(_db.CliClientes.AsNoTracking(), dto.idCliCliente, "Cliente", cancellationToken).ConfigureAwait(false);
            await EnsureExistsAsync(_db.SegUsuarios.AsNoTracking(), dto.idSegUsuario, "Usuario", cancellationToken).ConfigureAwait(false);
            await EnsureExistsAsync(_db.VenCatEstados.AsNoTracking(), dto.idVenCatEstado, "Estado de venta", cancellationToken).ConfigureAwait(false);

            var productIds = dto.Detalles.Select(d => d.idProProducto).Distinct().ToList();
            var productos = await _db.ProProductos.Where(e => productIds.Contains(e.id)).ToDictionaryAsync(e => e.id, cancellationToken).ConfigureAwait(false);
            foreach (var item in dto.Detalles)
            {
                if (!productos.TryGetValue(item.idProProducto, out var producto))
                {
                    throw new ValidationException($"Producto '{item.idProProducto}' no existe.");
                }

                if (producto.intNumeroExistencia < item.intPiezaVenta)
                {
                    throw new ConcurrencyConflictException();
                }
            }

            var clave = (dto.strClaveVenta ?? string.Empty).Trim();
            var venta = new VenVenta
            {
                idCliCliente = dto.idCliCliente,
                idSegUsuario = dto.idSegUsuario,
                idVenCatEstado = dto.idVenCatEstado,
                dteFechaHoraCompra = DateTime.UtcNow,
                strClaveVenta = clave,
            };

            // InMemory ignora transacciones (y lo eleva a error por warnings-as-errors):
            // la atomicidad en SQL va por Tx explícita; en InMemory por SaveChanges.
            var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
                : null;
            try
            {
                _db.VenVentas.Add(venta);
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                foreach (var item in dto.Detalles)
                {
                    var producto = productos[item.idProProducto];
                    producto.intNumeroExistencia -= item.intPiezaVenta;
                    _db.VenVentaDetalles.Add(new VenVentaDetalle
                    {
                        idVenVenta = venta.id,
                        idProProducto = item.idProProducto,
                        intPiezaVenta = item.intPiezaVenta,
                        decTotalVenta = producto.decPrecio * item.intPiezaVenta,
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

            await InvalidateAsync(venta.id, cancellationToken).ConfigureAwait(false);
            var created = await _db.VenVentas.AsNoTracking().FirstAsync(e => e.id == venta.id, cancellationToken).ConfigureAwait(false);
            var createdDetalles = await _db.VenVentaDetalles.AsNoTracking().Where(d => d.idVenVenta == venta.id).OrderBy(d => d.id).ToListAsync(cancellationToken).ConfigureAwait(false);
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

        private async Task InvalidateAsync(int? id = null, CancellationToken cancellationToken = default)
        {
            if (id.HasValue)
            {
                await _cache.RemoveAsync(CachePrefix, $"{ByIdKeyPrefix}{id.Value}", cancellationToken).ConfigureAwait(false);
            }

            var version = await _cache.GetAsync<int>(CachePrefix, VersionKey, cancellationToken).ConfigureAwait(false);
            await _cache.SetAsync(CachePrefix, VersionKey, version + 1, CacheTtl, cancellationToken).ConfigureAwait(false);
        }

        private static VenVentaDto ToDto(VenVenta entity, List<VenVentaDetalle> detalles)
        {
            return new VenVentaDto
            {
                id = entity.id,
                idCliCliente = entity.idCliCliente,
                idSegUsuario = entity.idSegUsuario,
                idVenCatEstado = entity.idVenCatEstado,
                dteFechaHoraCompra = entity.dteFechaHoraCompra,
                strClaveVenta = entity.strClaveVenta,
                Detalles = detalles.Select(d => new VenVentaDetalleDto
                {
                    id = d.id,
                    idProProducto = d.idProProducto,
                    intPiezaVenta = d.intPiezaVenta,
                    decTotalVenta = d.decTotalVenta,
                }).ToList(),
                RowVersion = entity.RowVersion,
            };
        }
    }
}
