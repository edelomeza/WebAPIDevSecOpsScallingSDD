using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebAPIDevSecOpsScallingSDD.Events;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface IPagoEventPublisher
    {
        Task PublishProcesadoAsync(PagoProcesadoEvent evt, CancellationToken cancellationToken = default);
    }

    public sealed class MassTransitPagoEventPublisher : IPagoEventPublisher
    {
        private readonly IEventBus _bus;

        public MassTransitPagoEventPublisher(IEventBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);
            _bus = bus;
        }

        public Task PublishProcesadoAsync(PagoProcesadoEvent evt, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(evt);
            return _bus.PublishAsync(evt, cancellationToken);
        }
    }

    // Fake intacto solo para UnitTest (fija el contrato: publica 1 vez / no publica en fallo).
    public sealed class FakePagoEventPublisher : IPagoEventPublisher
    {
        private readonly List<PagoProcesadoEvent> _published = new();

        public IReadOnlyList<PagoProcesadoEvent> Published => _published;

        public Task PublishProcesadoAsync(PagoProcesadoEvent evt, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(evt);
            _published.Add(evt);
            return Task.CompletedTask;
        }
    }
}
