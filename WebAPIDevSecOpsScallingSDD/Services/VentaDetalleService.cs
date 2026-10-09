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
    public interface IVentaDetalleService
    {
        Task<VenVentaDetalleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<VenVentaDetalleDto?> AddDetalleAsync(int idVenta, VenVentaDetalleCreateDto dto, string? callerUserId, CancellationToken cancellationToken = default);

        Task<bool> RemoveDetalleAsync(int id, VenVentaDetalleDeleteDto dto, string? callerUserId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProProductoAutocompleteDto>> AutocompleteProductoAsync(string texto, int maxResultados, CancellationToken cancellationToken = default);
    }

    public sealed class VentaDetalleService : IVentaDetalleService
    {
        private const string CachePrefix = "cache:";
        private const string VentaByIdKeyPrefix = "venta:";
        private const string VentaVersionKey = "venta:version";
        private const string ProductoByIdKeyPrefix = "producto:";
        private const string ProductoVersionKey = "producto:version";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;

        public VentaDetalleService(AppDbContext db, ICacheService cache)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            _db = db;
            _cache = cache;
        }

        public async Task<VenVentaDetalleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.VenVentaDetalles.AsNoTracking().FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            return entity is null ? null : ToDto(entity);
        }

        public async Task<VenVentaDetalleDto?> AddDetalleAsync(int idVenta, VenVentaDetalleCreateDto dto, string? callerUserId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var venta = await _db.VenVentas.FirstOrDefaultAsync(e => e.id == idVenta, cancellationToken).ConfigureAwait(false);
            if (venta is null)
            {
                return null;
            }

            EnsureOwner(venta.idSegUsuario, callerUserId);
            var producto = await _db.ProProductos.FirstOrDefaultAsync(e => e.id == dto.idProProducto, cancellationToken).ConfigureAwait(false);
            if (producto is null)
            {
                throw new ValidationException($"Producto '{dto.idProProducto}' no existe.");
            }

            if (producto.intNumeroExistencia < dto.intPiezaVenta)
            {
                throw new ConcurrencyConflictException();
            }

            // InMemory ignora transacciones (y lo eleva a error por warnings-as-errors):
            // la atomicidad en SQL va por Tx explícita; en InMemory por SaveChanges.
            var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
                : null;
            VenVentaDetalle detalle;
            try
            {
                producto.intNumeroExistencia -= dto.intPiezaVenta;
                detalle = new VenVentaDetalle
                {
                    idVenVenta = venta.id,
                    idProProducto = dto.idProProducto,
                    intPiezaVenta = dto.intPiezaVenta,
                    decTotalVenta = producto.decPrecio * dto.intPiezaVenta,
                };
                _db.VenVentaDetalles.Add(detalle);
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

            await InvalidateVentaAsync(venta.id, producto.id, cancellationToken).ConfigureAwait(false);
            return ToDto(detalle);
        }

        public async Task<bool> RemoveDetalleAsync(int id, VenVentaDetalleDeleteDto dto, string? callerUserId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var detalle = await _db.VenVentaDetalles.FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (detalle is null)
            {
                return false;
            }

            var venta = await _db.VenVentas.FirstOrDefaultAsync(e => e.id == detalle.idVenVenta, cancellationToken).ConfigureAwait(false);
            if (venta is null)
            {
                return false;
            }

            EnsureOwner(venta.idSegUsuario, callerUserId);
            _db.Entry(detalle).Property(e => e.RowVersion).OriginalValue = dto.RowVersion;
            var producto = await _db.ProProductos.FirstOrDefaultAsync(e => e.id == detalle.idProProducto, cancellationToken).ConfigureAwait(false);

            var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
                : null;
            try
            {
                if (producto is not null)
                {
                    producto.intNumeroExistencia += detalle.intPiezaVenta;
                }

                _db.VenVentaDetalles.Remove(detalle);
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

            await InvalidateVentaAsync(venta.id, detalle.idProProducto, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public async Task<IReadOnlyList<ProProductoAutocompleteDto>> AutocompleteProductoAsync(string texto, int maxResultados, CancellationToken cancellationToken = default)
        {
            var clean = (texto ?? string.Empty).Trim();
            if (clean.Length == 0)
            {
                return [];
            }

            var normalizado = maxResultados < 1 || maxResultados > 50 ? 10 : maxResultados;
            var version = await _cache.GetAsync<int>(CachePrefix, ProductoVersionKey, cancellationToken).ConfigureAwait(false);
            var key = $"producto:autocomplete:{version}:{clean}:{normalizado}";
            var cached = await _cache.GetAsync<IReadOnlyList<ProProductoAutocompleteDto>>(CachePrefix, key, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var items = await _db.ProProductos.AsNoTracking().Where(e => e.strNombreProducto.Contains(clean)).OrderBy(e => e.id).Take(normalizado).Select(e => new ProProductoAutocompleteDto
            {
                id = e.id,
                strNombreProducto = e.strNombreProducto,
            }).ToListAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<ProProductoAutocompleteDto> result = items;
            await _cache.SetAsync(CachePrefix, key, result, CacheTtl, cancellationToken).ConfigureAwait(false);
            return result;
        }

        private static void EnsureOwner(int ownerId, string? callerUserId)
        {
            var owned = $"{ownerId}";
            if (string.IsNullOrWhiteSpace(callerUserId) || !string.Equals(owned, callerUserId.Trim(), StringComparison.Ordinal))
            {
                throw new ForbiddenAccessException("El detalle no pertenece al usuario autenticado.");
            }
        }

        private async Task InvalidateVentaAsync(int idVenta, int idProducto, CancellationToken cancellationToken)
        {
            await _cache.RemoveAsync(CachePrefix, $"{VentaByIdKeyPrefix}{idVenta}", cancellationToken).ConfigureAwait(false);
            var ventaVersion = await _cache.GetAsync<int>(CachePrefix, VentaVersionKey, cancellationToken).ConfigureAwait(false);
            await _cache.SetAsync(CachePrefix, VentaVersionKey, ventaVersion + 1, CacheTtl, cancellationToken).ConfigureAwait(false);
            await _cache.RemoveAsync(CachePrefix, $"{ProductoByIdKeyPrefix}{idProducto}", cancellationToken).ConfigureAwait(false);
            var productoVersion = await _cache.GetAsync<int>(CachePrefix, ProductoVersionKey, cancellationToken).ConfigureAwait(false);
            await _cache.SetAsync(CachePrefix, ProductoVersionKey, productoVersion + 1, CacheTtl, cancellationToken).ConfigureAwait(false);
        }

        private static VenVentaDetalleDto ToDto(VenVentaDetalle entity)
        {
            return new VenVentaDetalleDto
            {
                id = entity.id,
                idProProducto = entity.idProProducto,
                intPiezaVenta = entity.intPiezaVenta,
                decTotalVenta = entity.decTotalVenta,
                RowVersion = entity.RowVersion,
            };
        }
    }
}
