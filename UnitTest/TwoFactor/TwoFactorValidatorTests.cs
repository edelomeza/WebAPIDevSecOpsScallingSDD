using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Validators;

namespace UnitTest.TwoFactor
{
    public class TwoFactorValidatorTests
    {
        [Fact]
        public void ValidSixDigitsPasses()
        {
            var validator = new TwoFactorVerifyRequestValidator();

            var result = validator.Validate(new TwoFactorVerifyRequest { TotpCode = "123456" });

            Assert.True(result.IsValid);
        }

        [Fact]
        public void EmptyAndMalformedFail()
        {
            var validator = new TwoFactorVerifyRequestValidator();

            Assert.False(validator.Validate(new TwoFactorVerifyRequest { TotpCode = string.Empty }).IsValid);
            Assert.False(validator.Validate(new TwoFactorVerifyRequest { TotpCode = "abc" }).IsValid);
            Assert.False(validator.Validate(new TwoFactorVerifyRequest { TotpCode = "12345" }).IsValid);
            Assert.False(validator.Validate(new TwoFactorVerifyRequest { TotpCode = "1234567" }).IsValid);
        }
    }
}
