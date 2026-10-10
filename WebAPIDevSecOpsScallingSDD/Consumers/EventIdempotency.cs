using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Models;

namespace WebAPIDevSecOpsScallingSDD.Consumers
{
    // Idempotencia del bus en 06-01. Regla: la marca se guarda en el MISMO SaveChanges que la
    // mutación; los caminos que lanzan para reintento o retornan sin mutar no dejan marca, así
    // el redelivery vuelve a entrar. El índice único en SQL es el guardián ante carreras.
    internal static class EventIdempotency
    {
        public static Task<bool> IsProcessedAsync(AppDbContext db, string evento, Guid pedidoId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentException.ThrowIfNullOrWhiteSpace(evento);
            return db.VenEventosProcesados.AsNoTracking().AnyAsync(e => e.strNombreEvento == evento && e.idPedido == pedidoId, cancellationToken);
        }

        public static void MarkProcessed(AppDbContext db, string evento, Guid pedidoId)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentException.ThrowIfNullOrWhiteSpace(evento);
            db.VenEventosProcesados.Add(new VenEventoProcesado
            {
                strNombreEvento = evento,
                idPedido = pedidoId,
                dteFechaProcesado = DateTime.UtcNow,
            });
        }
    }
}
