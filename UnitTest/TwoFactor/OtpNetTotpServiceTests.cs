using System;
using OtpNet;
using WebAPIDevSecOpsScallingSDD.Services;

namespace UnitTest.TwoFactor
{
    public class OtpNetTotpServiceTests
    {
        [Fact]
        public void GenerateSecretReturnsBase32AndUnique()
        {
            var service = new OtpNetTotpService();

            var first = service.GenerateSecret();
            var second = service.GenerateSecret();

            Assert.False(string.IsNullOrEmpty(first));
            Assert.NotEqual(first, second);
            var bytes = Base32Encoding.ToBytes(first);
            Assert.Equal(20, bytes.Length);
        }

        [Fact]
        public void BuildOtpAuthUriReturnsExpectedFormat()
        {
            var service = new OtpNetTotpService();

            var uri = service.BuildOtpAuthUri("JBSWY3DPEHPK3PXP", "ana@test.local", "WebAPI");

            Assert.StartsWith("otpauth://totp/", uri.ToString(), StringComparison.Ordinal);
            Assert.Contains("secret=JBSWY3DPEHPK3PXP", uri.ToString(), StringComparison.Ordinal);
            Assert.Contains("digits=6", uri.ToString(), StringComparison.Ordinal);
            Assert.Contains("period=30", uri.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public void NullArgumentsThrowArgumentNull()
        {
            var service = new OtpNetTotpService();

            Assert.Throws<ArgumentNullException>(() => service.Verify(null!, "123456"));
            Assert.Throws<ArgumentNullException>(() => service.Verify("JBSWY3DPEHPK3PXP", null!));
            Assert.Throws<ArgumentNullException>(() => service.BuildOtpAuthUri(null!, "a", "b"));
            Assert.Throws<ArgumentNullException>(() => service.BuildOtpAuthUri("s", null!, "b"));
            Assert.Throws<ArgumentNullException>(() => service.BuildOtpAuthUri("s", "a", null!));
        }

        [Fact]
        public void BlankArgumentsReturnFalseOrThrow()
        {
            var service = new OtpNetTotpService();

            Assert.False(service.Verify("   ", "123456"));
            Assert.False(service.Verify("JBSWY3DPEHPK3PXP", "   "));
            var secretEx = Assert.Throws<ArgumentException>(() => service.BuildOtpAuthUri("   ", "a", "b"));
            var accountEx = Assert.Throws<ArgumentException>(() => service.BuildOtpAuthUri("JBSWY3DPEHPK3PXP", "   ", "b"));
            var issuerEx = Assert.Throws<ArgumentException>(() => service.BuildOtpAuthUri("JBSWY3DPEHPK3PXP", "a", "   "));

            Assert.Equal("secret", secretEx.ParamName);
            Assert.Equal("account", accountEx.ParamName);
            Assert.Equal("issuer", issuerEx.ParamName);
            Assert.Contains("Secret is required.", secretEx.Message, StringComparison.Ordinal);
            Assert.Contains("Account is required.", accountEx.Message, StringComparison.Ordinal);
            Assert.Contains("Issuer is required.", issuerEx.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void VerifyCurrentCodeSucceedsAndWrongFails()
        {
            var service = new OtpNetTotpService();
            var secret = service.GenerateSecret();
            var current = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();

            Assert.True(service.Verify(secret, current));
            Assert.False(service.Verify(secret, "000000"));
        }

        [Fact]
        public void VerifyMalformedSecretReturnsFalse()
        {
            var service = new OtpNetTotpService();

            Assert.False(service.Verify("!!!no-base32!!!", "123456"));
        }
    }
}
