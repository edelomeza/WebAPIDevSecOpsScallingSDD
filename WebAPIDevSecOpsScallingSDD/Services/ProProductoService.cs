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
    public interface IProProductoService
    {
        Task<PagedResult<ProProductoDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        Task<ProProductoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<ProProductoDto> CreateAsync(ProProductoCreateDto dto, CancellationToken cancellationToken = default);

        Task<ProProductoDto?> UpdateAsync(int id, ProProductoUpdateDto dto, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, ProProductoDeleteDto dto, CancellationToken cancellationToken = default);

        Task<PagedResult<ProProductoDto>> SearchByNameAsync(string texto, int page, int pageSize, CancellationToken cancellationToken = default);
    }

    public sealed class ProProductoService : IProProductoService
    {
        private const string CachePrefix = "cache:";
        private const string ByIdKeyPrefix = "producto:";
        private const string VersionKey = "producto:version";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;

        public ProProductoService(AppDbContext db, ICacheService cache)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            _db = db;
            _cache = cache;
        }

        public async Task<PagedResult<ProProductoDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var version = await _cache.GetAsync<int>(CachePrefix, VersionKey, cancellationToken).ConfigureAwait(false);
            var key = $"producto:page:{version}:{page}:{pageSize}";
            var cached = await _cache.GetAsync<PagedResult<ProProductoDto>>(CachePrefix, key, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var query = _db.ProProductos.AsNoTracking().OrderBy(e => e.id);
            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(e => ToDto(e)).ToListAsync(cancellationToken).ConfigureAwait(false);
            var result = new PagedResult<ProProductoDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
            await _cache.SetAsync(CachePrefix, key, result, CacheTtl, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public async Task<ProProductoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync<ProProductoDto>(CachePrefix, $"{ByIdKeyPrefix}{id}", cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var entity = await _db.ProProductos.AsNoTracking().FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return null;
            }

            var dto = ToDto(entity);
            await _cache.SetAsync(CachePrefix, $"{ByIdKeyPrefix}{id}", dto, CacheTtl, cancellationToken).ConfigureAwait(false);
            return dto;
        }

        public async Task<ProProductoDto> CreateAsync(ProProductoCreateDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var entity = new ProProducto
            {
                strNombreProducto = dto.strNombreProducto,
                strURLImagen = dto.strURLImagen,
                strDescripcion = dto.strDescripcion,
                intNumeroExistencia = dto.intNumeroExistencia,
                decPrecio = dto.decPrecio,
                strCreadoPorUsuario = dto.strCreadoPorUsuario,
            };
            _db.ProProductos.Add(entity);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await InvalidateAsync(null, cancellationToken).ConfigureAwait(false);
            return ToDto(entity);
        }

        public async Task<ProProductoDto?> UpdateAsync(int id, ProProductoUpdateDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var entity = await _db.ProProductos.FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return null;
            }

            _db.Entry(entity).Property(e => e.RowVersion).OriginalValue = dto.RowVersion;
            entity.strNombreProducto = dto.strNombreProducto;
            entity.strURLImagen = dto.strURLImagen;
            entity.strDescripcion = dto.strDescripcion;
            entity.intNumeroExistencia = dto.intNumeroExistencia;
            entity.decPrecio = dto.decPrecio;
            entity.strCreadoPorUsuario = dto.strCreadoPorUsuario;
            try
            {
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyConflictException();
            }

            await InvalidateAsync(id, cancellationToken).ConfigureAwait(false);
            return ToDto(entity);
        }

        public async Task<bool> DeleteAsync(int id, ProProductoDeleteDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var entity = await _db.ProProductos.FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return false;
            }

            _db.Entry(entity).Property(e => e.RowVersion).OriginalValue = dto.RowVersion;
            _db.ProProductos.Remove(entity);
            try
            {
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyConflictException();
            }

            await InvalidateAsync(id, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public async Task<PagedResult<ProProductoDto>> SearchByNameAsync(string texto, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var clean = (texto ?? string.Empty).Trim();
            if (clean.Length == 0)
            {
                return new PagedResult<ProProductoDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
            }

            var version = await _cache.GetAsync<int>(CachePrefix, VersionKey, cancellationToken).ConfigureAwait(false);
            var key = $"producto:search:{version}:{clean}:{page}:{pageSize}";
            var cached = await _cache.GetAsync<PagedResult<ProProductoDto>>(CachePrefix, key, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var query = _db.ProProductos.AsNoTracking().Where(e => e.strNombreProducto.Contains(clean)).OrderBy(e => e.id);
            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(e => ToDto(e)).ToListAsync(cancellationToken).ConfigureAwait(false);
            var result = new PagedResult<ProProductoDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
            await _cache.SetAsync(CachePrefix, key, result, CacheTtl, cancellationToken).ConfigureAwait(false);
            return result;
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

        private static ProProductoDto ToDto(ProProducto entity)
        {
            return new ProProductoDto
            {
                id = entity.id,
                strNombreProducto = entity.strNombreProducto,
                strURLImagen = entity.strURLImagen,
                strDescripcion = entity.strDescripcion,
                intNumeroExistencia = entity.intNumeroExistencia,
                decPrecio = entity.decPrecio,
                strCreadoPorUsuario = entity.strCreadoPorUsuario,
                RowVersion = entity.RowVersion,
            };
        }
    }
}
