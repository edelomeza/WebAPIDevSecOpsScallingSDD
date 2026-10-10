using System;
using WebAPIDevSecOpsScallingSDD.Services;

namespace UnitTest.RateLimit
{
    public class RateLimitOptionsTests
    {
        [Fact]
        public void DefaultsMatchSpec()
        {
            var options = new RateLimitOptions();

            Assert.Equal(5, options.LoginPermitLimit);
            Assert.Equal(300, options.LoginWindowSeconds);
            Assert.Equal(10, options.Login2faPermitLimit);
            Assert.Equal(300, options.Login2faWindowSeconds);
            Assert.Equal(1000, options.GlobalPermitLimit);
            Assert.Equal(60, options.GlobalWindowSeconds);
            Assert.Equal(200, options.AdminPermitLimit);
            Assert.Equal(60, options.AdminWindowSeconds);
            Assert.Equal(10, options.ConcurrentWritesPermitLimit);
        }

        [Fact]
        public void ApplyMultiplierScalesAllSlidingLimits()
        {
            var options = new RateLimitOptions();

            options.ApplyMultiplier(3);

            Assert.Equal(15, options.LoginPermitLimit);
            Assert.Equal(30, options.Login2faPermitLimit);
            Assert.Equal(3000, options.GlobalPermitLimit);
            Assert.Equal(600, options.AdminPermitLimit);
            Assert.Equal(30, options.ConcurrentWritesPermitLimit);
        }

        [Fact]
        public void ApplyMultiplierBelowOneLeavesLimitsUnchanged()
        {
            var options = new RateLimitOptions();

            options.ApplyMultiplier(0);

            Assert.Equal(5, options.LoginPermitLimit);
            Assert.Equal(10, options.Login2faPermitLimit);
            Assert.Equal(1000, options.GlobalPermitLimit);
            Assert.Equal(200, options.AdminPermitLimit);
            Assert.Equal(10, options.ConcurrentWritesPermitLimit);
        }

        [Fact]
        public void PolicyNamesAreDistinctAndNonEmpty()
        {
            var names = new[]
            {
                RateLimitOptions.LoginPolicyName,
                RateLimitOptions.Login2faPolicyName,
                RateLimitOptions.GlobalPolicyName,
                RateLimitOptions.AdminPolicyName,
                RateLimitOptions.ConcurrentWritesPolicyName,
            };

            Assert.All(names, name => Assert.False(string.IsNullOrWhiteSpace(name)));
            Assert.Equal(names.Length, new System.Collections.Generic.HashSet<string>(names, StringComparer.Ordinal).Count);
        }
    }
}
