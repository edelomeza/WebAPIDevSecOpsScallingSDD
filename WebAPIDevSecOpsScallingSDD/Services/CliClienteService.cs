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
    public interface ICliClienteService
    {
        Task<PagedResult<CliClienteDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        Task<CliClienteDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<CliClienteDto> CreateAsync(CliClienteCreateDto dto, CancellationToken cancellationToken = default);

        Task<CliClienteDto?> UpdateAsync(int id, CliClienteUpdateDto dto, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CliClienteDeleteDto dto, CancellationToken cancellationToken = default);

        Task<PagedResult<CliClienteDto>> SearchByNameAsync(string texto, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<System.Collections.Generic.IReadOnlyList<CliClienteAutocompleteDto>> AutocompleteAsync(string texto, int maxResultados, CancellationToken cancellationToken = default);
    }

    public sealed class CliClienteService : ICliClienteService
    {
        private const string CachePrefix = "cache:";
        private const string ByIdKeyPrefix = "cliente:";
        private const string VersionKey = "cliente:version";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;

        public CliClienteService(AppDbContext db, ICacheService cache)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            _db = db;
            _cache = cache;
        }

        public async Task<PagedResult<CliClienteDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var version = await _cache.GetAsync<int>(CachePrefix, VersionKey, cancellationToken).ConfigureAwait(false);
            var key = $"cliente:page:{version}:{page}:{pageSize}";
            var cached = await _cache.GetAsync<PagedResult<CliClienteDto>>(CachePrefix, key, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var query = _db.CliClientes.AsNoTracking().OrderBy(e => e.id);
            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(e => ToDto(e)).ToListAsync(cancellationToken).ConfigureAwait(false);
            var result = new PagedResult<CliClienteDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
            await _cache.SetAsync(CachePrefix, key, result, CacheTtl, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public async Task<CliClienteDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync<CliClienteDto>(CachePrefix, $"{ByIdKeyPrefix}{id}", cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var entity = await _db.CliClientes.AsNoTracking().FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return null;
            }

            var dto = ToDto(entity);
            await _cache.SetAsync(CachePrefix, $"{ByIdKeyPrefix}{id}", dto, CacheTtl, cancellationToken).ConfigureAwait(false);
            return dto;
        }

        public async Task<CliClienteDto> CreateAsync(CliClienteCreateDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var entity = new CliCliente
            {
                strNombreCliente = dto.strNombreCliente,
                strDireccionCliente = dto.strDireccionCliente,
                strCorreoElectronico = dto.strCorreoElectronico,
                strNumeroTelefono = dto.strNumeroTelefono,
                strCreadoPorUsuario = dto.strCreadoPorUsuario,
            };
            _db.CliClientes.Add(entity);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await InvalidateAsync(null, cancellationToken).ConfigureAwait(false);
            return ToDto(entity);
        }

        public async Task<CliClienteDto?> UpdateAsync(int id, CliClienteUpdateDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var entity = await _db.CliClientes.FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return null;
            }

            _db.Entry(entity).Property(e => e.RowVersion).OriginalValue = dto.RowVersion;
            entity.strNombreCliente = dto.strNombreCliente;
            entity.strDireccionCliente = dto.strDireccionCliente;
            entity.strCorreoElectronico = dto.strCorreoElectronico;
            entity.strNumeroTelefono = dto.strNumeroTelefono;
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

        public async Task<bool> DeleteAsync(int id, CliClienteDeleteDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var entity = await _db.CliClientes.FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return false;
            }

            _db.Entry(entity).Property(e => e.RowVersion).OriginalValue = dto.RowVersion;
            _db.CliClientes.Remove(entity);
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

        public async Task<PagedResult<CliClienteDto>> SearchByNameAsync(string texto, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var clean = (texto ?? string.Empty).Trim();
            if (clean.Length == 0)
            {
                return new PagedResult<CliClienteDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
            }

            var version = await _cache.GetAsync<int>(CachePrefix, VersionKey, cancellationToken).ConfigureAwait(false);
            var key = $"cliente:search:{version}:{clean}:{page}:{pageSize}";
            var cached = await _cache.GetAsync<PagedResult<CliClienteDto>>(CachePrefix, key, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var query = _db.CliClientes.AsNoTracking().Where(e => e.strNombreCliente.Contains(clean)).OrderBy(e => e.id);
            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(e => ToDto(e)).ToListAsync(cancellationToken).ConfigureAwait(false);
            var result = new PagedResult<CliClienteDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
            await _cache.SetAsync(CachePrefix, key, result, CacheTtl, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public async Task<System.Collections.Generic.IReadOnlyList<CliClienteAutocompleteDto>> AutocompleteAsync(string texto, int maxResultados, CancellationToken cancellationToken = default)
        {
            var clean = (texto ?? string.Empty).Trim();
            if (clean.Length == 0)
            {
                return [];
            }

            var normalizado = maxResultados < 1 || maxResultados > 50 ? 10 : maxResultados;
            var version = await _cache.GetAsync<int>(CachePrefix, VersionKey, cancellationToken).ConfigureAwait(false);
            var key = $"cliente:autocomplete:{version}:{clean}:{normalizado}";
            var cached = await _cache.GetAsync<System.Collections.Generic.IReadOnlyList<CliClienteAutocompleteDto>>(CachePrefix, key, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var items = await _db.CliClientes.AsNoTracking().Where(e => e.strNombreCliente.Contains(clean)).OrderBy(e => e.id).Take(normalizado).Select(e => ToAutocompleteDto(e)).ToListAsync(cancellationToken).ConfigureAwait(false);
            System.Collections.Generic.IReadOnlyList<CliClienteAutocompleteDto> result = items;
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

        private static CliClienteAutocompleteDto ToAutocompleteDto(CliCliente entity)
        {
            return new CliClienteAutocompleteDto
            {
                id = entity.id,
                strNombreCliente = entity.strNombreCliente,
            };
        }

        private static CliClienteDto ToDto(CliCliente entity)
        {
            return new CliClienteDto
            {
                id = entity.id,
                strNombreCliente = entity.strNombreCliente,
                strDireccionCliente = entity.strDireccionCliente,
                strCorreoElectronico = entity.strCorreoElectronico,
                strNumeroTelefono = entity.strNumeroTelefono,
                strCreadoPorUsuario = entity.strCreadoPorUsuario,
                RowVersion = entity.RowVersion,
            };
        }
    }
}
