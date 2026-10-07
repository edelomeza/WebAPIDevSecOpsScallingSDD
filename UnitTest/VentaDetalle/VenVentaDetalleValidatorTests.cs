using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Validators;

namespace UnitTest.VentaDetalle
{
    public class VenVentaDetalleValidatorTests
    {
        [Fact]
        public void ValidDtosPass()
        {
            Assert.True(new VenVentaDetalleCreateValidator().Validate(new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 1 }).IsValid);
            Assert.True(new VenVentaDetalleDeleteValidator().Validate(new VenVentaDetalleDeleteDto { id = 1, RowVersion = new byte[] { 1 } }).IsValid);
        }

        [Fact]
        public void InvalidDtosFail()
        {
            Assert.False(new VenVentaDetalleCreateValidator().Validate(new VenVentaDetalleCreateDto { idProProducto = 0, intPiezaVenta = 1 }).IsValid);
            Assert.False(new VenVentaDetalleCreateValidator().Validate(new VenVentaDetalleCreateDto { idProProducto = 1, intPiezaVenta = 0 }).IsValid);
            Assert.False(new VenVentaDetalleDeleteValidator().Validate(new VenVentaDetalleDeleteDto { id = 0, RowVersion = new byte[] { 1 } }).IsValid);
        }
    }
}
