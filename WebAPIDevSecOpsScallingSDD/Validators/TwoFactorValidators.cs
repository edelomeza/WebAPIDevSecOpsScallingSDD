using FluentValidation;
using WebAPIDevSecOpsScallingSDD.Dtos;

namespace WebAPIDevSecOpsScallingSDD.Validators
{
    public sealed class TwoFactorVerifyRequestValidator : AbstractValidator<TwoFactorVerifyRequest>
    {
        public TwoFactorVerifyRequestValidator()
        {
            RuleFor(x => x.TotpCode).NotEmpty().Matches(@"^\d{6}$");
        }
    }
}
