using FluentValidation;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Validators
{
    public sealed class PedidoDetalleCreateValidator : AbstractValidator<PedidoDetalleCreateDto>
    {
        public PedidoDetalleCreateValidator()
        {
            RuleFor(x => x.idProProducto).GreaterThan(0);
            RuleFor(x => x.intCantidad).GreaterThan(0);
        }
    }

    public sealed class PedidoCreateValidator : AbstractValidator<PedidoCreateDto>
    {
        public PedidoCreateValidator()
        {
            RuleFor(x => x.idCliCliente).GreaterThan(0);
            RuleFor(x => x.Detalles).NotEmpty();
            RuleForEach(x => x.Detalles).SetValidator(new PedidoDetalleCreateValidator());
        }
    }
}
