using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Consumers;

namespace UnitTest.Consumers
{
    public class EventIdempotencyTests
    {
        [Fact]
        public async Task IsProcessedNullDbThrowsArgumentNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => EventIdempotency.IsProcessedAsync(null!, "E", Guid.NewGuid()));
        }

        [Fact]
        public async Task IsProcessedBlankEventoThrowsArgument()
        {
            await using var context = ConsumerSeeds.CreateContext();

            await Assert.ThrowsAsync<ArgumentException>(() => EventIdempotency.IsProcessedAsync(context, string.Empty, Guid.NewGuid()));
            await Assert.ThrowsAsync<ArgumentException>(() => EventIdempotency.IsProcessedAsync(context, "   ", Guid.NewGuid()));
        }

        [Fact]
        public void MarkProcessedNullDbThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => EventIdempotency.MarkProcessed(null!, "E", Guid.NewGuid()));
        }

        [Fact]
        public void MarkProcessedBlankEventoThrowsArgument()
        {
            using var context = ConsumerSeeds.CreateContext();

            Assert.Throws<ArgumentException>(() => EventIdempotency.MarkProcessed(context, string.Empty, Guid.NewGuid()));
        }

        [Fact]
        public async Task AbsentReturnsFalseAndMarkMakesItTrue()
        {
            await using var context = ConsumerSeeds.CreateContext();
            var pedidoId = Guid.NewGuid();

            Assert.False(await context.VenEventosProcesados.CountAsync(e => e.idPedido == pedidoId) > 0);
            Assert.False(await EventIdempotency.IsProcessedAsync(context, "E", pedidoId));

            EventIdempotency.MarkProcessed(context, "E", pedidoId);
            await context.SaveChangesAsync();

            Assert.True(await EventIdempotency.IsProcessedAsync(context, "E", pedidoId));
        }

        [Fact]
        public async Task SameEventoDifferentPedidoIsNotProcessed()
        {
            await using var context = ConsumerSeeds.CreateContext();
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            EventIdempotency.MarkProcessed(context, "E", first);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            Assert.False(await EventIdempotency.IsProcessedAsync(context, "E", second));
        }

        [Fact]
        public async Task DifferentEventoSamePedidoIsNotProcessed()
        {
            await using var context = ConsumerSeeds.CreateContext();
            var pedidoId = Guid.NewGuid();
            EventIdempotency.MarkProcessed(context, "E1", pedidoId);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            Assert.False(await EventIdempotency.IsProcessedAsync(context, "E2", pedidoId));
        }
    }
}
