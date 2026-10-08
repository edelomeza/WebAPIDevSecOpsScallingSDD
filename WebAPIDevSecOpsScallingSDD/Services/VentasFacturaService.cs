using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface IVentasFacturaService
    {
        Task<VenPedidoFacturaResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }

    public sealed class VentasFacturaService : IVentasFacturaService
    {
        private const string CachePrefix = "cache:";
        private const string ByIdKeyPrefix = "factura:";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;

        public VentasFacturaService(AppDbContext db, ICacheService cache)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            _db = db;
            _cache = cache;
        }

        public async Task<VenPedidoFacturaResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync<VenPedidoFacturaResponseDto>(CachePrefix, $"{ByIdKeyPrefix}{id}", cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            // NOTE (06-02): FacturaConsumer + eventos FacturaGeneradoEvent/FacturaRechazadaEvent en fase 06.
            // NOTE (06-04): strEstado real desde la máquina de estados; lectura sin transición.
            // NOTE (04-04): rate-limit vía UseRateLimiter en fase 04.
            var entity = await _db.VenPedidoFacturas.AsNoTracking().FirstOrDefaultAsync(e => e.id == id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return null;
            }

            var dto = ToDto(entity);
            await _cache.SetAsync(CachePrefix, $"{ByIdKeyPrefix}{id}", dto, CacheTtl, cancellationToken).ConfigureAwait(false);
            return dto;
        }

        private static VenPedidoFacturaResponseDto ToDto(Models.VenPedidoFactura entity)
        {
            return new VenPedidoFacturaResponseDto
            {
                id = entity.id,
                idVenPedido = entity.idVenPedido,
                strFolioFactura = entity.strFolioFactura,
                strRFC = entity.strRFC,
                decTotal = entity.decTotal,
                dteFechaEmision = entity.dteFechaEmision,
                strEstado = entity.strEstado,
                RowVersion = entity.RowVersion,
            };
        }
    }
}
