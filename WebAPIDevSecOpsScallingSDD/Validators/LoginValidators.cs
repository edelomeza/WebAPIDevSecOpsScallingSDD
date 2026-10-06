using FluentValidation;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Validators
{
    public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.strNombre).NotEmpty().MaximumLength(50);
            RuleFor(x => x.strPasswordPlano).NotEmpty().MaximumLength(200);
        }
    }
}
