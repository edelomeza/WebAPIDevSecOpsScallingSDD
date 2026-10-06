using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Validators;

namespace UnitTest.Venta
{
    public class VenVentaValidatorTests
    {
        [Fact]
        public void ValidPayloadPasses()
        {
            var validator = new VenVentaCreateValidator();

            var result = validator.Validate(new VenVentaCreateDto
            {
                idCliCliente = 1,
                idSegUsuario = 1,
                idVenCatEstado = 1,
                strClaveVenta = "V-00000001",
                Detalles = new[] { new VenVentaDetalleCreateItemDto { idProProducto = 1, intPiezaVenta = 1 } },
            });

            Assert.True(result.IsValid);
        }

        [Fact]
        public void InvalidPayloadsFail()
        {
            var validator = new VenVentaCreateValidator();

            var zeroIds = validator.Validate(new VenVentaCreateDto
            {
                idCliCliente = 0,
                idSegUsuario = 0,
                idVenCatEstado = 0,
                strClaveVenta = "V-00000001",
                Detalles = new[] { new VenVentaDetalleCreateItemDto { idProProducto = 1, intPiezaVenta = 1 } },
            });
            var emptyClave = validator.Validate(new VenVentaCreateDto
            {
                idCliCliente = 1,
                idSegUsuario = 1,
                idVenCatEstado = 1,
                strClaveVenta = string.Empty,
                Detalles = new[] { new VenVentaDetalleCreateItemDto { idProProducto = 1, intPiezaVenta = 1 } },
            });
            var longClave = validator.Validate(new VenVentaCreateDto
            {
                idCliCliente = 1,
                idSegUsuario = 1,
                idVenCatEstado = 1,
                strClaveVenta = "CLAVE-MUY-LARGA",
                Detalles = new[] { new VenVentaDetalleCreateItemDto { idProProducto = 1, intPiezaVenta = 1 } },
            });
            var emptyDetalles = validator.Validate(new VenVentaCreateDto
            {
                idCliCliente = 1,
                idSegUsuario = 1,
                idVenCatEstado = 1,
                strClaveVenta = "V-00000001",
                Detalles = [],
            });
            var zeroItem = validator.Validate(new VenVentaCreateDto
            {
                idCliCliente = 1,
                idSegUsuario = 1,
                idVenCatEstado = 1,
                strClaveVenta = "V-00000001",
                Detalles = new[] { new VenVentaDetalleCreateItemDto { idProProducto = 0, intPiezaVenta = 0 } },
            });

            Assert.False(zeroIds.IsValid);
            Assert.False(emptyClave.IsValid);
            Assert.False(longClave.IsValid);
            Assert.False(emptyDetalles.IsValid);
            Assert.False(zeroItem.IsValid);
        }
    }
}
