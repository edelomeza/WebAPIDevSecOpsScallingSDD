using FluentValidation;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Validators
{
    public sealed class SegUsuarioCreateValidator : AbstractValidator<SegUsuarioCreateDto>
    {
        public SegUsuarioCreateValidator()
        {
            RuleFor(x => x.strNombre).NotEmpty().MaximumLength(50);
            RuleFor(x => x.strCorreoElectronico).NotEmpty().MaximumLength(50).EmailAddress();
            RuleFor(x => x.strPasswordPlano).NotEmpty().MinimumLength(8).MaximumLength(200);
        }
    }

    public sealed class SegUsuarioUpdateValidator : AbstractValidator<SegUsuarioUpdateDto>
    {
        public SegUsuarioUpdateValidator()
        {
            RuleFor(x => x.id).GreaterThan(0);
            RuleFor(x => x.strNombre).NotEmpty().MaximumLength(50);
            RuleFor(x => x.strCorreoElectronico).NotEmpty().MaximumLength(50).EmailAddress();
            RuleFor(x => x.RowVersion).NotNull().Must(version => version.Length > 0);
        }
    }

    public sealed class SegUsuarioDeleteValidator : AbstractValidator<SegUsuarioDeleteDto>
    {
        public SegUsuarioDeleteValidator()
        {
            RuleFor(x => x.id).GreaterThan(0);
            RuleFor(x => x.RowVersion).NotNull().Must(version => version.Length > 0);
        }
    }
}
