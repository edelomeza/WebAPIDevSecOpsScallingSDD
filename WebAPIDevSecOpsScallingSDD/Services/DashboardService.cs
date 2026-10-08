using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface IVentasDashboardService
    {
        Task<DashboardDto> GetAsync(DashboardFilterDto? filter, CancellationToken cancellationToken = default);
    }

    public sealed class VentasDashboardService : IVentasDashboardService
    {
        private const string CachePrefix = "cache:";
        private const string KeyPrefix = "dashboard:";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;

        public VentasDashboardService(AppDbContext db, ICacheService cache)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(cache);
            _db = db;
            _cache = cache;
        }

        public async Task<DashboardDto> GetAsync(DashboardFilterDto? filter, CancellationToken cancellationToken = default)
        {
            // NOTE (06-04): estados temporales ("Creado"/"Procesado"/"Emitida"); 06-04 fija la máquina de estados.
            // NOTE (08-01): métricas detalladas OTel/Prometheus en fase 08; aquí solo agregados DB.
            // NOTE (04-04): rate-limit vía UseRateLimiter en fase 04.
            var active = filter ?? new DashboardFilterDto();
            var estado = string.IsNullOrWhiteSpace(active.EstadoSaga) ? null : active.EstadoSaga.Trim();
            var cacheKey = $"{KeyPrefix}{ToKeyPart(active.Desde)}:{ToKeyPart(active.Hasta)}:{SanitizeEstado(estado)}";

            var cached = await _cache.GetAsync<DashboardDto>(CachePrefix, cacheKey, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var pedidos = _db.VenPedidos.AsNoTracking();
            if (active.Desde.HasValue)
            {
                var desde = active.Desde.Value;
                pedidos = pedidos.Where(p => p.dteFechaPedido >= desde);
            }

            if (active.Hasta.HasValue)
            {
                var hasta = active.Hasta.Value;
                pedidos = pedidos.Where(p => p.dteFechaPedido <= hasta);
            }

            if (estado is not null)
            {
                pedidos = pedidos.Where(p => p.strEstadoSaga == estado);
            }

            var pagos = _db.VenPedidoPagos.AsNoTracking();
            if (active.Desde.HasValue)
            {
                var desde = active.Desde.Value;
                pagos = pagos.Where(p => p.dteFechaPago >= desde);
            }

            if (active.Hasta.HasValue)
            {
                var hasta = active.Hasta.Value;
                pagos = pagos.Where(p => p.dteFechaPago <= hasta);
            }

            var facturas = _db.VenPedidoFacturas.AsNoTracking();
            if (active.Desde.HasValue)
            {
                var desde = active.Desde.Value;
                facturas = facturas.Where(f => f.dteFechaEmision >= desde);
            }

            if (active.Hasta.HasValue)
            {
                var hasta = active.Hasta.Value;
                facturas = facturas.Where(f => f.dteFechaEmision <= hasta);
            }

            var totalPedidos = await pedidos.CountAsync(cancellationToken).ConfigureAwait(false);
            var montoPedidos = await pedidos.SumAsync(p => (decimal?)p.decTotal, cancellationToken).ConfigureAwait(false) ?? 0m;
            var porEstado = await pedidos
                .GroupBy(p => p.strEstadoSaga)
                .Select(g => new EstadoConteoDto { Estado = g.Key, Total = g.Count() })
                .OrderBy(e => e.Estado)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var totalPagos = await pagos.CountAsync(cancellationToken).ConfigureAwait(false);
            var montoPagos = await pagos.SumAsync(p => (decimal?)p.decMonto, cancellationToken).ConfigureAwait(false) ?? 0m;
            var totalFacturas = await facturas.CountAsync(cancellationToken).ConfigureAwait(false);
            var montoFacturas = await facturas.SumAsync(f => (decimal?)f.decTotal, cancellationToken).ConfigureAwait(false) ?? 0m;

            // NOTE (06-01): sin bus/SQS hasta fase 06; profundidad fake 0.
            var dto = new DashboardDto
            {
                TotalPedidos = totalPedidos,
                TotalPagos = totalPagos,
                TotalFacturas = totalFacturas,
                MontoTotalPedidos = montoPedidos,
                MontoTotalPagos = montoPagos,
                MontoTotalFacturas = montoFacturas,
                PorEstadoSaga = porEstado,
                ProfundidadCola = 0,
            };

            await _cache.SetAsync(CachePrefix, cacheKey, dto, CacheTtl, cancellationToken).ConfigureAwait(false);
            return dto;
        }

        private static string ToKeyPart(DateTime? value)
        {
            return value.HasValue ? value.Value.Ticks.ToString(CultureInfo.InvariantCulture) : "null";
        }

        private static string SanitizeEstado(string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
            {
                return "null";
            }

            var clean = estado.Trim();
            clean = clean.Replace("password", "_", StringComparison.OrdinalIgnoreCase);
            clean = clean.Replace("secret", "_", StringComparison.OrdinalIgnoreCase);
            clean = clean.Replace("token", "_", StringComparison.OrdinalIgnoreCase);
            clean = clean.Replace(":", "-", StringComparison.Ordinal);
            return clean;
        }
    }
}
