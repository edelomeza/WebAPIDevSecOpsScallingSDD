using FluentValidation;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Validators
{
    public sealed class VenVentaDetalleCreateItemValidator : AbstractValidator<VenVentaDetalleCreateItemDto>
    {
        public VenVentaDetalleCreateItemValidator()
        {
            RuleFor(x => x.idProProducto).GreaterThan(0);
            RuleFor(x => x.intPiezaVenta).GreaterThan(0);
        }
    }

    public sealed class VenVentaCreateValidator : AbstractValidator<VenVentaCreateDto>
    {
        public VenVentaCreateValidator()
        {
            RuleFor(x => x.idCliCliente).GreaterThan(0);
            RuleFor(x => x.idSegUsuario).GreaterThan(0);
            RuleFor(x => x.idVenCatEstado).GreaterThan(0);
            RuleFor(x => x.strClaveVenta).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Detalles).NotEmpty();
            RuleForEach(x => x.Detalles).SetValidator(new VenVentaDetalleCreateItemValidator());
        }
    }
}
