using FluentValidation;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Validators
{
    public sealed class PagoCreateValidator : AbstractValidator<PagoCreateDto>
    {
        public PagoCreateValidator()
        {
            RuleFor(x => x.idVenPedido).NotEmpty();
            RuleFor(x => x.decMonto).GreaterThan(0);
            RuleFor(x => x.strMetodoPago).MaximumLength(50);
            RuleFor(x => x.strIdTransaccion).MaximumLength(100);
        }
    }
}
