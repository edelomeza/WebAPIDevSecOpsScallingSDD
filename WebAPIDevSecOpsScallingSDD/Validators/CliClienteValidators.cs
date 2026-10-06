using FluentValidation;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Validators
{
    public sealed class CliClienteCreateValidator : AbstractValidator<CliClienteCreateDto>
    {
        public CliClienteCreateValidator()
        {
            RuleFor(x => x.strNombreCliente).NotEmpty().MaximumLength(100);
            RuleFor(x => x.strDireccionCliente).MaximumLength(200);
            RuleFor(x => x.strCorreoElectronico).NotEmpty().EmailAddress().MaximumLength(100);
            RuleFor(x => x.strNumeroTelefono).NotEmpty().Length(10);
            RuleFor(x => x.strCreadoPorUsuario).MaximumLength(50);
        }
    }

    public sealed class CliClienteUpdateValidator : AbstractValidator<CliClienteUpdateDto>
    {
        public CliClienteUpdateValidator()
        {
            RuleFor(x => x.id).GreaterThan(0);
            RuleFor(x => x.strNombreCliente).NotEmpty().MaximumLength(100);
            RuleFor(x => x.strDireccionCliente).MaximumLength(200);
            RuleFor(x => x.strCorreoElectronico).NotEmpty().EmailAddress().MaximumLength(100);
            RuleFor(x => x.strNumeroTelefono).NotEmpty().Length(10);
            RuleFor(x => x.strCreadoPorUsuario).MaximumLength(50);
            RuleFor(x => x.RowVersion).NotNull().Must(version => version.Length > 0);
        }
    }

    public sealed class CliClienteDeleteValidator : AbstractValidator<CliClienteDeleteDto>
    {
        public CliClienteDeleteValidator()
        {
            RuleFor(x => x.id).GreaterThan(0);
            RuleFor(x => x.RowVersion).NotNull().Must(version => version.Length > 0);
        }
    }
}
