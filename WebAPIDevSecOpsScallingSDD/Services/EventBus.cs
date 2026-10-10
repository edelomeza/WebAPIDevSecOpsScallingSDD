using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class;
    }

    public sealed class MassTransitEventBus : IEventBus
    {
        private readonly IPublishEndpoint _publish;

        public MassTransitEventBus(IPublishEndpoint publish)
        {
            ArgumentNullException.ThrowIfNull(publish);
            _publish = publish;
        }

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(message);
            return _publish.Publish(message, cancellationToken);
        }
    }
}
