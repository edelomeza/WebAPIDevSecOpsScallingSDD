using FluentValidation;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Validators
{
    public sealed class VenVentaDetalleCreateValidator : AbstractValidator<VenVentaDetalleCreateDto>
    {
        public VenVentaDetalleCreateValidator()
        {
            RuleFor(x => x.idProProducto).GreaterThan(0);
            RuleFor(x => x.intPiezaVenta).GreaterThan(0);
        }
    }

    public sealed class VenVentaDetalleDeleteValidator : AbstractValidator<VenVentaDetalleDeleteDto>
    {
        public VenVentaDetalleDeleteValidator()
        {
            RuleFor(x => x.id).GreaterThan(0);
        }
    }
}
