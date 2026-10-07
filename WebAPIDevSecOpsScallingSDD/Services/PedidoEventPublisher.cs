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

    // NOTE (06-01): fake InMemory del bus; 06-01 lo reemplaza por MassTransit (InMemory local / SQS prod).
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
