using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Models;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    /// <summary>Bloqueo persistente de login (04-02): 5 fallos → 15 min. Sustituye al lockout en caché efímera.</summary>
    public interface ILoginLockoutStore
    {
        Task<bool> IsLockedAsync(string nombre, CancellationToken cancellationToken = default);

        Task RecordFailureAsync(string nombre, CancellationToken cancellationToken = default);

        Task ResetAsync(string nombre, CancellationToken cancellationToken = default);
    }

    public sealed class EfLoginLockoutStore : ILoginLockoutStore
    {
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private const int MaxAttempts = 5;

        private readonly AppDbContext _db;
        private readonly TimeProvider _clock;

        public EfLoginLockoutStore(AppDbContext db, TimeProvider clock)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(clock);
            _db = db;
            _clock = clock;
        }

        public async Task<bool> IsLockedAsync(string nombre, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(nombre);
            var row = await _db.SegBloqueos.AsNoTracking().FirstOrDefaultAsync(e => e.strNombre == nombre, cancellationToken).ConfigureAwait(false);
            if (row?.dteBloqueoHasta is null)
            {
                return false;
            }

            return _clock.GetUtcNow().UtcDateTime < row.dteBloqueoHasta.Value;
        }

        public async Task RecordFailureAsync(string nombre, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(nombre);
            var now = _clock.GetUtcNow().UtcDateTime;
            var row = await _db.SegBloqueos.FirstOrDefaultAsync(e => e.strNombre == nombre, cancellationToken).ConfigureAwait(false);
            if (row is null)
            {
                _db.SegBloqueos.Add(new SegBloqueo { strNombre = nombre, intIntentosFallidos = 1 });
            }
            else if (row.dteBloqueoHasta is not null && now >= row.dteBloqueoHasta.Value)
            {
                row.intIntentosFallidos = 1;
                row.dteBloqueoHasta = null;
            }
            else
            {
                row.intIntentosFallidos++;
                if (row.intIntentosFallidos >= MaxAttempts)
                {
                    row.dteBloqueoHasta = now + LockoutDuration;
                }
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Un único reintento ante ráfagas concurrentes (RowVersion): recargar y reaplicar.
                // Si vuelve a colisionar, otra petición ya contó el intento: no evade el bloqueo.
                foreach (var entry in _db.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                var fresh = await _db.SegBloqueos.FirstOrDefaultAsync(e => e.strNombre == nombre, cancellationToken).ConfigureAwait(false);
                if (fresh is null)
                {
                    _db.SegBloqueos.Add(new SegBloqueo { strNombre = nombre, intIntentosFallidos = 1 });
                }
                else if (fresh.dteBloqueoHasta is not null && now >= fresh.dteBloqueoHasta.Value)
                {
                    fresh.intIntentosFallidos = 1;
                    fresh.dteBloqueoHasta = null;
                }
                else
                {
                    fresh.intIntentosFallidos++;
                    if (fresh.intIntentosFallidos >= MaxAttempts)
                    {
                        fresh.dteBloqueoHasta = now + LockoutDuration;
                    }
                }

                try
                {
                    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DbUpdateConcurrencyException)
                {
                    foreach (var entry in _db.ChangeTracker.Entries())
                    {
                        entry.State = EntityState.Detached;
                    }
                }
            }
        }

        public async Task ResetAsync(string nombre, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(nombre);
            var row = await _db.SegBloqueos.FirstOrDefaultAsync(e => e.strNombre == nombre, cancellationToken).ConfigureAwait(false);
            if (row is null || (row.intIntentosFallidos == 0 && row.dteBloqueoHasta is null))
            {
                return;
            }

            row.intIntentosFallidos = 0;
            row.dteBloqueoHasta = null;
            try
            {
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                foreach (var entry in _db.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }
            }
        }
    }
}
