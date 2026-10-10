using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebAPIDevSecOpsScallingSDD.Events;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface IPedidoEventPublisher
    {
        Task PublishAsync(PedidoCreadoEvent evt, CancellationToken cancellationToken = default);
    }

    public sealed class MassTransitPedidoEventPublisher : IPedidoEventPublisher
    {
        private readonly IEventBus _bus;

        public MassTransitPedidoEventPublisher(IEventBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);
            _bus = bus;
        }

        public Task PublishAsync(PedidoCreadoEvent evt, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(evt);
            return _bus.PublishAsync(evt, cancellationToken);
        }
    }

    // Fake intacto solo para UnitTest (los tests fijan el contrato: publica 1 vez / no publica en fallo).
    public sealed class FakePedidoEventPublisher : IPedidoEventPublisher
    {
        private readonly List<PedidoCreadoEvent> _published = new();

        public IReadOnlyList<PedidoCreadoEvent> Published => _published;

        public Task PublishAsync(PedidoCreadoEvent evt, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(evt);
            _published.Add(evt);
            return Task.CompletedTask;
        }
    }
}
