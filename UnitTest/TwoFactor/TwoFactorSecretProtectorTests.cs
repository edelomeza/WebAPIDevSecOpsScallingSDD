using System;
using Microsoft.AspNetCore.DataProtection;
using WebAPIDevSecOpsScallingSDD.Services;

namespace UnitTest.TwoFactor
{
    public class TwoFactorSecretProtectorTests
    {
        private static readonly IDataProtectionProvider TestProvider = DataProtectionProvider.Create("Test-Protector");

        private static TwoFactorSecretProtector CreateProtector() => new TwoFactorSecretProtector(TestProvider);

        [Fact]
        public void NullProviderThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new TwoFactorSecretProtector(null!));
        }

        [Fact]
        public void ProtectUnprotectRoundtripsAndDiffersFromRaw()
        {
            var protector = CreateProtector();

            var @protected = protector.Protect("JBSWY3DPEHPK3PXP");

            Assert.NotEqual("JBSWY3DPEHPK3PXP", @protected);
            Assert.Equal("JBSWY3DPEHPK3PXP", protector.Unprotect(@protected));
            Assert.DoesNotContain("JBSWY3DPEHPK3PXP", @protected, StringComparison.Ordinal);
        }

        [Fact]
        public void BlankSecretsThrowArgumentException()
        {
            var protector = CreateProtector();

            var secretEx = Assert.Throws<ArgumentException>(() => protector.Protect("   "));
            var payloadEx = Assert.Throws<ArgumentException>(() => protector.Unprotect("   "));

            Assert.Equal("secret", secretEx.ParamName);
            Assert.Equal("protectedPayload", payloadEx.ParamName);
            Assert.Contains("Secret is required.", secretEx.Message, StringComparison.Ordinal);
            Assert.Contains("Protected payload is required.", payloadEx.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void NullSecretsThrowArgumentNull()
        {
            var protector = CreateProtector();

            Assert.Throws<ArgumentNullException>(() => protector.Protect(null!));
            Assert.Throws<ArgumentNullException>(() => protector.Unprotect(null!));
        }

        [Fact]
        public void UnprotectTamperedThrowsCryptographicException()
        {
            var protector = CreateProtector();
            var @protected = protector.Protect("JBSWY3DPEHPK3PXP");

            Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(() => protector.Unprotect(@protected + "tampered"));
        }

        [Fact]
        public void TrimsWhitespaceBeforeProtecting()
        {
            var protector = CreateProtector();

            var @protected = protector.Protect("  JBSWY3DPEHPK3PXP  ");

            Assert.Equal("JBSWY3DPEHPK3PXP", protector.Unprotect(@protected));
        }
    }
}
