using FluentValidation;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Validators
{
    public sealed class VenCatEstadoCreateValidator : AbstractValidator<VenCatEstadoCreateDto>
    {
        public VenCatEstadoCreateValidator()
        {
            RuleFor(x => x.strValor).NotEmpty().MaximumLength(50);
            RuleFor(x => x.strDescripcion).MaximumLength(200);
        }
    }

    public sealed class VenCatEstadoUpdateValidator : AbstractValidator<VenCatEstadoUpdateDto>
    {
        public VenCatEstadoUpdateValidator()
        {
            RuleFor(x => x.id).GreaterThan(0);
            RuleFor(x => x.strValor).NotEmpty().MaximumLength(50);
            RuleFor(x => x.strDescripcion).MaximumLength(200);
        }
    }

    public sealed class VenCatEstadoDeleteValidator : AbstractValidator<VenCatEstadoDeleteDto>
    {
        public VenCatEstadoDeleteValidator()
        {
            RuleFor(x => x.id).GreaterThan(0);
        }
    }
}
