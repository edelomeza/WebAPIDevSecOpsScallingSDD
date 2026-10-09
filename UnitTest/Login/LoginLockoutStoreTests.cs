using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using WebAPIDevSecOpsScallingSDD.Models;
using WebAPIDevSecOpsScallingSDD.Services;

namespace UnitTest.Login
{
    public class LoginLockoutStoreTests
    {
        [Fact]
        public async Task RecordFailureRetriesOnceAfterConcurrencyConflictOnFreshRow()
        {
            var now = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
            await using var context = CreateFlakyContext(throwFirstSaves: 1);
            var store = new EfLoginLockoutStore(context, new TestTimeProvider(now));

            await store.RecordFailureAsync("R1");

            var row = await context.SegBloqueos.AsNoTracking().FirstOrDefaultAsync(e => e.strNombre == "R1");
            Assert.NotNull(row);
            Assert.Equal("R1", row.strNombre);
            Assert.Equal(1, row.intIntentosFallidos);
            Assert.Null(row.dteBloqueoHasta);
        }

        [Fact]
        public async Task RecordFailureRetryMergesWithExistingRow()
        {
            var now = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
            await using var context = CreateFlakyContext(throwFirstSaves: 0);
            context.SegBloqueos.Add(new SegBloqueo { strNombre = "R2", intIntentosFallidos = 4 });
            await context.SaveChangesAsync();
            context.ArmFailures(1);
            var store = new EfLoginLockoutStore(context, new TestTimeProvider(now));

            await store.RecordFailureAsync("R2");

            var row = await context.SegBloqueos.AsNoTracking().FirstOrDefaultAsync(e => e.strNombre == "R2");
            Assert.NotNull(row);
            Assert.Equal(5, row.intIntentosFallidos);
            Assert.Equal(now.UtcDateTime + TimeSpan.FromMinutes(15), row.dteBloqueoHasta);
        }

        [Fact]
        public async Task RecordFailureRetryRearmsExpiredRow()
        {
            var now = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
            await using var context = CreateFlakyContext(throwFirstSaves: 0);
            context.SegBloqueos.Add(new SegBloqueo { strNombre = "R3", intIntentosFallidos = 5, dteBloqueoHasta = now.UtcDateTime });
            await context.SaveChangesAsync();
            context.ArmFailures(1);
            var store = new EfLoginLockoutStore(context, new TestTimeProvider(now));

            await store.RecordFailureAsync("R3");

            var row = await context.SegBloqueos.AsNoTracking().FirstOrDefaultAsync(e => e.strNombre == "R3");
            Assert.NotNull(row);
            Assert.Equal(1, row.intIntentosFallidos);
            Assert.Null(row.dteBloqueoHasta);
        }

        [Fact]
        public async Task RecordFailureSurvivesDoubleConcurrencyConflict()
        {
            var now = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
            await using var context = CreateFlakyContext(throwFirstSaves: 0);
            context.SegBloqueos.Add(new SegBloqueo { strNombre = "R4", intIntentosFallidos = 2 });
            await context.SaveChangesAsync();
            context.ArmFailures(2);
            var store = new EfLoginLockoutStore(context, new TestTimeProvider(now));

            await store.RecordFailureAsync("R4");

            Assert.False(context.ChangeTracker.HasChanges());
        }

        [Fact]
        public async Task ResetIgnoresConcurrencyConflict()
        {
            var now = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
            await using var context = CreateFlakyContext(throwFirstSaves: 0);
            context.SegBloqueos.Add(new SegBloqueo { strNombre = "R5", intIntentosFallidos = 3 });
            await context.SaveChangesAsync();
            context.ArmFailures(1);
            var store = new EfLoginLockoutStore(context, new TestTimeProvider(now));

            await store.ResetAsync("R5");

            Assert.False(context.ChangeTracker.HasChanges());
        }

        [Fact]
        public async Task ResetOnCleanRowIsNoOp()
        {
            await using var context = CreateContext();
            context.SegBloqueos.Add(new SegBloqueo { strNombre = "R6", intIntentosFallidos = 0 });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var store = new EfLoginLockoutStore(context, TestTimeProvider.Fixed());

            await store.ResetAsync("R6");
            await store.ResetAsync("Nadie");

            Assert.False(context.ChangeTracker.HasChanges());
        }

        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static FlakyDbContext CreateFlakyContext(int throwFirstSaves)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new FlakyDbContext(options, throwFirstSaves);
        }

        private sealed class TestTimeProvider : TimeProvider
        {
            private DateTimeOffset _now;

            public TestTimeProvider(DateTimeOffset now)
            {
                _now = now;
            }

            public static TestTimeProvider Fixed()
            {
                return new TestTimeProvider(new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero));
            }

            public override DateTimeOffset GetUtcNow() => _now;
        }

        private sealed class FlakyDbContext : AppDbContext
        {
            private int _failuresLeft;

            public FlakyDbContext(DbContextOptions<AppDbContext> options, int throwFirstSaves)
                : base(options)
            {
                _failuresLeft = throwFirstSaves;
            }

            public void ArmFailures(int count) => _failuresLeft = count;

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                if (_failuresLeft > 0)
                {
                    _failuresLeft--;
                    throw new DbUpdateConcurrencyException();
                }

                return base.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
