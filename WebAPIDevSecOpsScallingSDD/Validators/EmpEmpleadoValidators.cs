using FluentValidation;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Validators
{
    public sealed class EmpEmpleadoCreateValidator : AbstractValidator<EmpEmpleadoCreateDto>
    {
        internal const string CurpPattern = "^[A-Za-z]{4}[0-9]{6}[HhMm][A-Za-z]{5}[A-Za-z0-9][0-9]$";

        public EmpEmpleadoCreateValidator()
        {
            RuleFor(x => x.strNombre).NotEmpty().MaximumLength(50);
            RuleFor(x => x.strAPaterno).MaximumLength(50);
            RuleFor(x => x.strAMaterno).MaximumLength(50);
            RuleFor(x => x.strCURP).MaximumLength(18).Matches(CurpPattern).When(x => !string.IsNullOrEmpty(x.strCURP));
            RuleFor(x => x.idEmpCatTipoEmpleado).GreaterThan(0).When(x => x.idEmpCatTipoEmpleado.HasValue);
        }
    }

    public sealed class EmpEmpleadoUpdateValidator : AbstractValidator<EmpEmpleadoUpdateDto>
    {
        public EmpEmpleadoUpdateValidator()
        {
            RuleFor(x => x.id).GreaterThan(0);
            RuleFor(x => x.strNombre).NotEmpty().MaximumLength(50);
            RuleFor(x => x.strAPaterno).MaximumLength(50);
            RuleFor(x => x.strAMaterno).MaximumLength(50);
            RuleFor(x => x.strCURP).MaximumLength(18).Matches(EmpEmpleadoCreateValidator.CurpPattern).When(x => !string.IsNullOrEmpty(x.strCURP));
            RuleFor(x => x.idEmpCatTipoEmpleado).GreaterThan(0).When(x => x.idEmpCatTipoEmpleado.HasValue);
            RuleFor(x => x.RowVersion).NotNull().Must(version => version.Length > 0);
        }
    }

    public sealed class EmpEmpleadoDeleteValidator : AbstractValidator<EmpEmpleadoDeleteDto>
    {
        public EmpEmpleadoDeleteValidator()
        {
            RuleFor(x => x.id).GreaterThan(0);
            RuleFor(x => x.RowVersion).NotNull().Must(version => version.Length > 0);
        }
    }
}
