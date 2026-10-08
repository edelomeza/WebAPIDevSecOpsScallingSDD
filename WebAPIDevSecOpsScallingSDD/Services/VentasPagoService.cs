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
    public interface IVentasPagoService
    {
        Task<PagoResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PagoResponseDto>> GetByPedidoIdAsync(Guid pedidoId, CancellationToken cancellationToken = default);

        Task<PagoResponseDto> CreateAsync(PagoCreateDto dto, CancellationToken cancellationToken = default);
    }

    public sealed class VentasPagoService : IVentasPagoService
    {
        private const string CachePrefix = "cache:";
        private const string ByIdKeyPrefix = "pago:";
        private const string ByPedidoKeyPrefix = "pago:pedido:";
        private const string VersionKey = "pago:version";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;

        public VentasPagoService(AppDbContext db, ICacheService cache)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            _db = db;
            _cache = cache;
        }

        public async Task<PagoResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync<PagoResponseDto>(CachePrefix, $"{ByIdKeyPrefix}{id}", cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var entity = await _db.VenPedidoPagos.AsNoTracking().FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return null;
            }

            var dto = ToDto(entity);
            await _cache.SetAsync(CachePrefix, $"{ByIdKeyPrefix}{id}", dto, CacheTtl, cancellationToken).ConfigureAwait(false);
            return dto;
        }

        public async Task<IReadOnlyList<PagoResponseDto>> GetByPedidoIdAsync(Guid pedidoId, CancellationToken cancellationToken = default)
        {
            var version = await _cache.GetAsync<int>(CachePrefix, VersionKey, cancellationToken).ConfigureAwait(false);
            var cached = await _cache.GetAsync<List<PagoResponseDto>>(CachePrefix, $"{ByPedidoKeyPrefix}{version}:{pedidoId}", cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var entities = await _db.VenPedidoPagos.AsNoTracking().Where(e => e.idVenPedido == pedidoId).OrderBy(e => e.id).ToListAsync(cancellationToken).ConfigureAwait(false);
            var dtos = entities.Select(ToDto).ToList();
            await _cache.SetAsync(CachePrefix, $"{ByPedidoKeyPrefix}{version}:{pedidoId}", dtos, CacheTtl, cancellationToken).ConfigureAwait(false);
            return dtos;
        }

        public async Task<PagoResponseDto> CreateAsync(PagoCreateDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var pedidoExists = await _db.VenPedidos.AsNoTracking().AnyAsync(e => e.id == dto.idVenPedido, cancellationToken).ConfigureAwait(false);
            if (!pedidoExists)
            {
                throw new ValidationException($"Pedido '{dto.idVenPedido}' no existe.");
            }

            var transaccion = NormalizeOptional(dto.strIdTransaccion);
            if (transaccion is not null)
            {
                var duplicate = await _db.VenPedidoPagos.AsNoTracking().AnyAsync(e => e.strIdTransaccion == transaccion, cancellationToken).ConfigureAwait(false);
                if (duplicate)
                {
                    throw new ConcurrencyConflictException($"La transacción '{transaccion}' ya fue registrada.");
                }
            }

            var pago = new VenPedidoPago
            {
                idVenPedido = dto.idVenPedido,
                decMonto = dto.decMonto,
                strMetodoPago = NormalizeOptional(dto.strMetodoPago),
                strIdTransaccion = transaccion,
                // NOTE (06-04): estado temporal; 06-04 fija la máquina de estados de la saga.
                strEstado = "Procesado",
                dteFechaPago = DateTime.UtcNow,
            };

            try
            {
                _db.VenPedidoPagos.Add(pago);
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConcurrencyConflictException("El pago fue modificado por otro proceso.", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new ConcurrencyConflictException("El pago fue modificado por otro proceso.", ex);
            }

            await InvalidateAsync(pago.id, cancellationToken).ConfigureAwait(false);

            var created = await _db.VenPedidoPagos.AsNoTracking().FirstAsync(e => e.id == pago.id, cancellationToken).ConfigureAwait(false);
            return ToDto(created);
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

        private static string? NormalizeOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static PagoResponseDto ToDto(VenPedidoPago entity)
        {
            return new PagoResponseDto
            {
                id = entity.id,
                idVenPedido = entity.idVenPedido,
                decMonto = entity.decMonto,
                strMetodoPago = entity.strMetodoPago,
                strIdTransaccion = entity.strIdTransaccion,
                strEstado = entity.strEstado,
                dteFechaPago = entity.dteFechaPago,
                RowVersion = entity.RowVersion,
            };
        }
    }
}
