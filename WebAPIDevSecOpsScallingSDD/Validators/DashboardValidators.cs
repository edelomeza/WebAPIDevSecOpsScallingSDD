using FluentValidation;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Validators
{
    public sealed class DashboardFilterValidator : AbstractValidator<DashboardFilterDto>
    {
        public DashboardFilterValidator()
        {
            RuleFor(x => x.Hasta)
                .Must((dto, hasta) => dto.Desde is null || hasta is null || hasta.Value >= dto.Desde.Value)
                .WithMessage("El rango de fechas es inválido.");
            RuleFor(x => x.EstadoSaga).MaximumLength(50);
        }
    }
}
