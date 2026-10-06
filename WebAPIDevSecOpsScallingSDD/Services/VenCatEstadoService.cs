using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Models;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface IVenCatEstadoService
    {
        Task<PagedResult<VenCatEstadoDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        Task<VenCatEstadoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<VenCatEstadoDto> CreateAsync(VenCatEstadoCreateDto dto, CancellationToken cancellationToken = default);

        Task<VenCatEstadoDto?> UpdateAsync(int id, VenCatEstadoUpdateDto dto, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, VenCatEstadoDeleteDto dto, CancellationToken cancellationToken = default);
    }

    public sealed class VenCatEstadoService : IVenCatEstadoService
    {
        private const string CachePrefix = "cache:";
        private const string ByIdKeyPrefix = "estado-venta:";
        private const string VersionKey = "estado-venta:version";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;

        public VenCatEstadoService(AppDbContext db, ICacheService cache)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            _db = db;
            _cache = cache;
        }

        public async Task<PagedResult<VenCatEstadoDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var version = await _cache.GetAsync<int>(CachePrefix, VersionKey, cancellationToken).ConfigureAwait(false);
            var key = $"estado-venta:page:{version}:{page}:{pageSize}";
            var cached = await _cache.GetAsync<PagedResult<VenCatEstadoDto>>(CachePrefix, key, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var query = _db.VenCatEstados.AsNoTracking().OrderBy(e => e.id);
            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(e => ToDto(e)).ToListAsync(cancellationToken).ConfigureAwait(false);
            var result = new PagedResult<VenCatEstadoDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
            await _cache.SetAsync(CachePrefix, key, result, CacheTtl, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public async Task<VenCatEstadoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync<VenCatEstadoDto>(CachePrefix, $"{ByIdKeyPrefix}{id}", cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var entity = await _db.VenCatEstados.AsNoTracking().FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return null;
            }

            var dto = ToDto(entity);
            await _cache.SetAsync(CachePrefix, $"{ByIdKeyPrefix}{id}", dto, CacheTtl, cancellationToken).ConfigureAwait(false);
            return dto;
        }

        public async Task<VenCatEstadoDto> CreateAsync(VenCatEstadoCreateDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var entity = new VenCatEstado
            {
                strValor = dto.strValor,
                strDescripcion = dto.strDescripcion,
            };
            _db.VenCatEstados.Add(entity);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await InvalidateAsync(null, cancellationToken).ConfigureAwait(false);
            return ToDto(entity);
        }

        public async Task<VenCatEstadoDto?> UpdateAsync(int id, VenCatEstadoUpdateDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var entity = await _db.VenCatEstados.FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return null;
            }

            entity.strValor = dto.strValor;
            entity.strDescripcion = dto.strDescripcion;
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await InvalidateAsync(id, cancellationToken).ConfigureAwait(false);
            return ToDto(entity);
        }

        public async Task<bool> DeleteAsync(int id, VenCatEstadoDeleteDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var entity = await _db.VenCatEstados.FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return false;
            }

            _db.VenCatEstados.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await InvalidateAsync(id, cancellationToken).ConfigureAwait(false);
            return true;
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

        private static VenCatEstadoDto ToDto(VenCatEstado entity)
        {
            return new VenCatEstadoDto
            {
                id = entity.id,
                strValor = entity.strValor,
                strDescripcion = entity.strDescripcion,
            };
        }
    }
}
