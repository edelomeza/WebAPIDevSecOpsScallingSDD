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
    public interface ISegUsuarioService
    {
        Task<PagedResult<SegUsuarioDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        Task<SegUsuarioDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<SegUsuarioDto> CreateAsync(SegUsuarioCreateDto dto, CancellationToken cancellationToken = default);

        Task<SegUsuarioDto?> UpdateAsync(int id, SegUsuarioUpdateDto dto, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, SegUsuarioDeleteDto dto, CancellationToken cancellationToken = default);

        Task<PagedResult<SegUsuarioDto>> SearchByNameAsync(string texto, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<System.Collections.Generic.IReadOnlyList<SegUsuarioAutocompleteDto>> AutocompleteAsync(string texto, int maxResultados, CancellationToken cancellationToken = default);
    }

    public sealed class SegUsuarioService : ISegUsuarioService
    {
        private const string CachePrefix = "cache:";
        private const string ByIdKeyPrefix = "usuario:";
        private const string VersionKey = "usuario:version";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;
        private readonly ISegUsuarioPasswordHasher _hasher;

        public SegUsuarioService(AppDbContext db, ICacheService cache, ISegUsuarioPasswordHasher hasher)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(hasher);
            _db = db;
            _cache = cache;
            _hasher = hasher;
        }

        public async Task<PagedResult<SegUsuarioDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var version = await _cache.GetAsync<int>(CachePrefix, VersionKey, cancellationToken).ConfigureAwait(false);
            var key = $"usuario:page:{version}:{page}:{pageSize}";
            var cached = await _cache.GetAsync<PagedResult<SegUsuarioDto>>(CachePrefix, key, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var query = _db.SegUsuarios.AsNoTracking().OrderBy(e => e.id);
            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(e => ToDto(e)).ToListAsync(cancellationToken).ConfigureAwait(false);
            var result = new PagedResult<SegUsuarioDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
            await _cache.SetAsync(CachePrefix, key, result, CacheTtl, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public async Task<SegUsuarioDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync<SegUsuarioDto>(CachePrefix, $"{ByIdKeyPrefix}{id}", cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var entity = await _db.SegUsuarios.AsNoTracking().FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return null;
            }

            var dto = ToDto(entity);
            await _cache.SetAsync(CachePrefix, $"{ByIdKeyPrefix}{id}", dto, CacheTtl, cancellationToken).ConfigureAwait(false);
            return dto;
        }

        public async Task<SegUsuarioDto> CreateAsync(SegUsuarioCreateDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var entity = new SegUsuario
            {
                strNombre = dto.strNombre,
                strCorreoElectronico = dto.strCorreoElectronico,
                strPWD = _hasher.Hash(dto.strPasswordPlano),
                dteFechaRegistro = DateTime.UtcNow,
                bln2FAHabilitado = false,
                str2FASecreto = null,
            };
            _db.SegUsuarios.Add(entity);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await InvalidateAsync(null, cancellationToken).ConfigureAwait(false);
            return ToDto(entity);
        }

        public async Task<SegUsuarioDto?> UpdateAsync(int id, SegUsuarioUpdateDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var entity = await _db.SegUsuarios.FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return null;
            }

            _db.Entry(entity).Property(e => e.RowVersion).OriginalValue = dto.RowVersion;
            entity.strNombre = dto.strNombre;
            entity.strCorreoElectronico = dto.strCorreoElectronico;
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

        public async Task<bool> DeleteAsync(int id, SegUsuarioDeleteDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var entity = await _db.SegUsuarios.FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return false;
            }

            _db.Entry(entity).Property(e => e.RowVersion).OriginalValue = dto.RowVersion;
            _db.SegUsuarios.Remove(entity);
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

        public async Task<PagedResult<SegUsuarioDto>> SearchByNameAsync(string texto, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var clean = (texto ?? string.Empty).Trim();
            if (clean.Length == 0)
            {
                return new PagedResult<SegUsuarioDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
            }

            var version = await _cache.GetAsync<int>(CachePrefix, VersionKey, cancellationToken).ConfigureAwait(false);
            var key = $"usuario:search:{version}:{clean}:{page}:{pageSize}";
            var cached = await _cache.GetAsync<PagedResult<SegUsuarioDto>>(CachePrefix, key, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var query = _db.SegUsuarios.AsNoTracking().Where(e => e.strNombre.Contains(clean)).OrderBy(e => e.id);
            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(e => ToDto(e)).ToListAsync(cancellationToken).ConfigureAwait(false);
            var result = new PagedResult<SegUsuarioDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
            await _cache.SetAsync(CachePrefix, key, result, CacheTtl, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public async Task<System.Collections.Generic.IReadOnlyList<SegUsuarioAutocompleteDto>> AutocompleteAsync(string texto, int maxResultados, CancellationToken cancellationToken = default)
        {
            var clean = (texto ?? string.Empty).Trim();
            if (clean.Length == 0)
            {
                return [];
            }

            var normalizado = maxResultados < 1 || maxResultados > 50 ? 10 : maxResultados;
            var version = await _cache.GetAsync<int>(CachePrefix, VersionKey, cancellationToken).ConfigureAwait(false);
            var key = $"usuario:autocomplete:{version}:{clean}:{normalizado}";
            var cached = await _cache.GetAsync<System.Collections.Generic.IReadOnlyList<SegUsuarioAutocompleteDto>>(CachePrefix, key, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var items = await _db.SegUsuarios.AsNoTracking().Where(e => e.strNombre.Contains(clean)).OrderBy(e => e.id).Take(normalizado).Select(e => ToAutocompleteDto(e)).ToListAsync(cancellationToken).ConfigureAwait(false);
            System.Collections.Generic.IReadOnlyList<SegUsuarioAutocompleteDto> result = items;
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

        private static SegUsuarioDto ToDto(SegUsuario entity)
        {
            return new SegUsuarioDto
            {
                id = entity.id,
                strNombre = entity.strNombre,
                strCorreoElectronico = entity.strCorreoElectronico,
                dteFechaRegistro = entity.dteFechaRegistro,
                bln2FAHabilitado = entity.bln2FAHabilitado,
                RowVersion = entity.RowVersion,
            };
        }

        private static SegUsuarioAutocompleteDto ToAutocompleteDto(SegUsuario entity)
        {
            return new SegUsuarioAutocompleteDto
            {
                id = entity.id,
                strNombre = entity.strNombre,
            };
        }
    }
}
