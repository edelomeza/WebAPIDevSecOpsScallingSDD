using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using WebAPIDevSecOpsScallingSDD.Events;
using WebAPIDevSecOpsScallingSDD.Services;

namespace UnitTest.Consumers
{
    public class EventBusTests
    {
        [Fact]
        public void NullPublishEndpointThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new MassTransitEventBus(null!));
            Assert.Throws<ArgumentNullException>(() => new MassTransitPedidoEventPublisher(null!));
            Assert.Throws<ArgumentNullException>(() => new MassTransitPagoEventPublisher(null!));
        }

        [Fact]
        public async Task NullMessageThrowsArgumentNull()
        {
            var endpoint = new RecordingPublishEndpoint();
            var bus = new MassTransitEventBus(endpoint);
            var pedidos = new MassTransitPedidoEventPublisher(bus);
            var pagos = new MassTransitPagoEventPublisher(bus);

            await Assert.ThrowsAsync<ArgumentNullException>(() => bus.PublishAsync<object>(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => pedidos.PublishAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => pagos.PublishProcesadoAsync(null!));
        }

        [Fact]
        public async Task PublisherGuardsFireBeforeDelegating()
        {
            // Sin la guarda propia, el nulo llegaría al bus (el doble permisivo no valida).
            var bus = new PassThroughBus();
            var pedidos = new MassTransitPedidoEventPublisher(bus);
            var pagos = new MassTransitPagoEventPublisher(bus);
            var pedidoId = Guid.NewGuid();

            await Assert.ThrowsAsync<ArgumentNullException>(() => pedidos.PublishAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => pagos.PublishProcesadoAsync(null!));
            await pedidos.PublishAsync(new PedidoCreadoEvent { PedidoId = pedidoId, ClienteId = 1, Total = 10m });
            await pagos.PublishProcesadoAsync(new PagoProcesadoEvent { PedidoId = pedidoId, Monto = 10m });

            Assert.Equal(2, bus.Calls);
        }

        private sealed class PassThroughBus : IEventBus
        {
            public int Calls { get; private set; }

            public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
                where T : class
            {
                Calls++;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task ForwardsSameInstanceToTransport()
        {
            var endpoint = new RecordingPublishEndpoint();
            var bus = new MassTransitEventBus(endpoint);
            var pedidos = new MassTransitPedidoEventPublisher(bus);
            var pagos = new MassTransitPagoEventPublisher(bus);

            var pedido = new PedidoCreadoEvent { PedidoId = Guid.NewGuid(), ClienteId = 1, Total = 20m };
            var pago = new PagoProcesadoEvent { PedidoId = pedido.PedidoId, Monto = 20m };

            await bus.PublishAsync(pedido);
            await pedidos.PublishAsync(pedido);
            await pagos.PublishProcesadoAsync(pago);

            Assert.Equal(3, endpoint.Published.Count);
            Assert.Same(pedido, endpoint.Published[0]);
            Assert.Same(pedido, endpoint.Published[1]);
            Assert.Same(pago, endpoint.Published[2]);
        }

        [Fact]
        public async Task FakePagoPublisherNullThrowsAndRecords()
        {
            var fake = new FakePagoEventPublisher();

            await Assert.ThrowsAsync<ArgumentNullException>(() => fake.PublishProcesadoAsync(null!));

            var evt = new PagoProcesadoEvent { PedidoId = Guid.NewGuid(), Monto = 10m };
            await fake.PublishProcesadoAsync(evt);

            Assert.Same(evt, Assert.Single(fake.Published));
        }

        private sealed class RecordingPublishEndpoint : IPublishEndpoint
        {
            public readonly List<object> Published = new();

            public Task Publish<T>(T message, CancellationToken cancellationToken = default)
                where T : class
            {
                // Sin guarda a propósito: si el SUT pierde la suya, este stub debe dejar pasar
                // el nulo para que el test lo cace (Stryker).
                Published.Add(message!);
                return Task.CompletedTask;
            }

            public Task Publish(object message, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task Publish<T>(object values, CancellationToken cancellationToken = default)
                where T : class
            {
                throw new NotSupportedException();
            }

            public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
                where T : class
            {
                throw new NotSupportedException();
            }

            public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
                where T : class
            {
                throw new NotSupportedException();
            }

            public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
                where T : class
            {
                throw new NotSupportedException();
            }

            public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
                where T : class
            {
                throw new NotSupportedException();
            }

            public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
            {
                throw new NotSupportedException();
            }
        }
    }
}
