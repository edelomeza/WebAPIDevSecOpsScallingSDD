using System;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Validators;

namespace UnitTest.VentasDashboard
{
    public class DashboardFilterValidatorTests
    {
        [Fact]
        public void EmptyFilterIsValid()
        {
            var validator = new DashboardFilterValidator();

            var result = validator.Validate(new DashboardFilterDto());

            Assert.True(result.IsValid);
        }

        [Fact]
        public void HastaBeforeDesdeIsInvalid()
        {
            var validator = new DashboardFilterValidator();
            var filter = new DashboardFilterDto
            {
                Desde = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
                Hasta = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            };

            var result = validator.Validate(filter);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void EqualBoundsAreValid()
        {
            var pivot = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);
            var validator = new DashboardFilterValidator();

            var result = validator.Validate(new DashboardFilterDto { Desde = pivot, Hasta = pivot });

            Assert.True(result.IsValid);
        }

        [Fact]
        public void EstadoSagaTooLongIsInvalid()
        {
            var validator = new DashboardFilterValidator();

            var result = validator.Validate(new DashboardFilterDto { EstadoSaga = new string('E', 51) });

            Assert.False(result.IsValid);
        }

        [Fact]
        public void EstadoSagaMaxLengthIsValid()
        {
            var validator = new DashboardFilterValidator();

            var result = validator.Validate(new DashboardFilterDto { EstadoSaga = new string('E', 50) });

            Assert.True(result.IsValid);
        }
    }
}
