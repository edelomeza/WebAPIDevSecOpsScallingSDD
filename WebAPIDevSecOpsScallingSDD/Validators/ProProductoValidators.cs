using FluentValidation;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Validators
{
    public sealed class ProProductoCreateValidator : AbstractValidator<ProProductoCreateDto>
    {
        public ProProductoCreateValidator()
        {
            RuleFor(x => x.strNombreProducto).NotEmpty().MaximumLength(50);
            RuleFor(x => x.strURLImagen).MaximumLength(300);
            RuleFor(x => x.strDescripcion).MaximumLength(250);
            RuleFor(x => x.intNumeroExistencia).GreaterThanOrEqualTo(0);
            RuleFor(x => x.decPrecio).GreaterThanOrEqualTo(0);
            RuleFor(x => x.strCreadoPorUsuario).MaximumLength(50);
        }
    }

    public sealed class ProProductoUpdateValidator : AbstractValidator<ProProductoUpdateDto>
    {
        public ProProductoUpdateValidator()
        {
            RuleFor(x => x.id).GreaterThan(0);
            RuleFor(x => x.strNombreProducto).NotEmpty().MaximumLength(50);
            RuleFor(x => x.strURLImagen).MaximumLength(300);
            RuleFor(x => x.strDescripcion).MaximumLength(250);
            RuleFor(x => x.intNumeroExistencia).GreaterThanOrEqualTo(0);
            RuleFor(x => x.decPrecio).GreaterThanOrEqualTo(0);
            RuleFor(x => x.strCreadoPorUsuario).MaximumLength(50);
            RuleFor(x => x.RowVersion).NotNull().Must(version => version.Length > 0);
        }
    }

    public sealed class ProProductoDeleteValidator : AbstractValidator<ProProductoDeleteDto>
    {
        public ProProductoDeleteValidator()
        {
            RuleFor(x => x.id).GreaterThan(0);
            RuleFor(x => x.RowVersion).NotNull().Must(version => version.Length > 0);
        }
    }
}
