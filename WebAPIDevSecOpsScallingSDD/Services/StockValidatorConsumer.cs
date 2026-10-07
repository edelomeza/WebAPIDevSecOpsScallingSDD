using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    // NOTE (06-02): stub de solo-lectura del validador de stock; 06-02 lo cablea al bus y a la compensación.
    public sealed class StockValidatorConsumer
    {
        private readonly AppDbContext _db;

        public StockValidatorConsumer(AppDbContext db)
        {
            ArgumentNullException.ThrowIfNull(db);
            _db = db;
        }

        public async Task<bool> HasStockAsync(Guid pedidoId, CancellationToken cancellationToken = default)
        {
            var detalles = await _db.VenPedidoDetalles.AsNoTracking().Where(d => d.idVenPedido == pedidoId).ToListAsync(cancellationToken).ConfigureAwait(false);
            if (detalles.Count == 0)
            {
                return false;
            }

            var productIds = detalles.Select(d => d.idProProducto).Distinct().ToList();
            var existencias = await _db.ProProductos.Where(e => productIds.Contains(e.id)).ToDictionaryAsync(e => e.id, e => e.intNumeroExistencia, cancellationToken).ConfigureAwait(false);
            return detalles.All(d => existencias.TryGetValue(d.idProProducto, out var existencia) && existencia >= d.intCantidad);
        }
    }
}
