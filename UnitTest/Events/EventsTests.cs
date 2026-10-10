using System;
using System.Linq;
using WebAPIDevSecOpsScallingSDD.Events;

namespace UnitTest.Events
{
    public class EventsTests
    {
        [Fact]
        public void AllSevenContractsAreVersioned()
        {
            Assert.Equal(1, PedidoCreadoEvent.SchemaVersion);
            Assert.Equal(1, StockValidadoEvent.SchemaVersion);
            Assert.Equal(1, StockRechazadoEvent.SchemaVersion);
            Assert.Equal(1, PagoProcesadoEvent.SchemaVersion);
            Assert.Equal(1, PagoRechazadoEvent.SchemaVersion);
            Assert.Equal(1, FacturaGeneradoEvent.SchemaVersion);
            Assert.Equal(1, FacturaRechazadaEvent.SchemaVersion);
        }

        [Fact]
        public void PayloadsAreImmutableRecords()
        {
            var payloads = new[]
            {
                typeof(PedidoCreadoEvent), typeof(StockValidadoEvent), typeof(StockRechazadoEvent),
                typeof(PagoProcesadoEvent), typeof(PagoRechazadoEvent),
                typeof(FacturaGeneradoEvent), typeof(FacturaRechazadaEvent),
            };

            Assert.All(payloads, type =>
            {
                var data = type.GetProperties().Where(p => p.Name != "EqualityContract");
                Assert.NotEmpty(data);
                Assert.All(data, property => Assert.True(IsInitOnly(property), $"Propiedad {type.Name}.{property.Name} debe ser init-only."));
            });
        }

        private static bool IsInitOnly(System.Reflection.PropertyInfo property)
        {
            var setter = property.GetSetMethod();
            return setter is null || setter.ReturnParameter.GetRequiredCustomModifiers().Any(modifier => string.Equals(modifier.Name, "IsExternalInit", StringComparison.Ordinal));
        }

        [Fact]
        public void PayloadsContainNoSecretsOrPersonalData()
        {
            var forbidden = new[] { "password", "secret", "token", "totp", "pwd", "rfc", "curp", "correo", "tarjeta" };
            var payloads = new[]
            {
                typeof(PedidoCreadoEvent), typeof(StockValidadoEvent), typeof(StockRechazadoEvent),
                typeof(PagoProcesadoEvent), typeof(PagoRechazadoEvent),
                typeof(FacturaGeneradoEvent), typeof(FacturaRechazadaEvent),
            };

            Assert.All(payloads, type => Assert.All(
                type.GetProperties().Select(p => p.Name),
                name => Assert.DoesNotContain(forbidden, fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase))));
        }

        [Fact]
        public void PedidoCreadoKeepsGuidIdentity()
        {
            var evt = new PedidoCreadoEvent { PedidoId = Guid.NewGuid(), ClienteId = 1, Total = 20m };

            Assert.Equal(typeof(Guid), typeof(PedidoCreadoEvent).GetProperty("PedidoId")!.PropertyType);
            Assert.NotEqual(Guid.Empty, evt.PedidoId);
        }
    }
}
