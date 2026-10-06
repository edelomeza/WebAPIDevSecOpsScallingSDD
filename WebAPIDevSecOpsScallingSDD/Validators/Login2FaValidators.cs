using FluentValidation;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Validators
{
    public sealed class Login2FaVerifyRequestValidator : AbstractValidator<Login2FaVerifyRequest>
    {
        public Login2FaVerifyRequestValidator()
        {
            RuleFor(x => x.TempToken).NotEmpty().MaximumLength(128);
            RuleFor(x => x.TotpCode).NotEmpty().Matches(@"^\d{6}$");
        }
    }
}
