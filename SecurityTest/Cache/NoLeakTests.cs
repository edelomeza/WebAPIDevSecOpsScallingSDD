using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebAPIDevSecOpsScallingSDD.Services;

namespace SecurityTest.Cache
{
    public class NoLeakTests
    {
        [Theory]
        [InlineData("cache:", "k-password-k")]
        [InlineData("cache:", "k-secret-k")]
        [InlineData("cache:", "k-token-k")]
        public async Task ForbiddenKeyPatternsAreRejectedFromDiCache(string prefix, string key)
        {
            using var factory = new WebApplicationFactory<Program>();
            var cache = factory.Services.GetRequiredService<ICacheService>();

            await Assert.ThrowsAsync<InvalidOperationException>(() => cache.SetAsync(prefix, key, "x", TimeSpan.FromMinutes(1)));
        }

        [Fact]
        public void Argon2idHashEmbedsNoPlaintextAndUsesRandomSalt()
        {
            var hasher = new Argon2IdSegUsuarioPasswordHasher();
            const string plano = "Secreto123";

            var first = hasher.Hash(plano);
            var second = hasher.Hash(plano);

            Assert.DoesNotContain(plano, first, StringComparison.Ordinal);
            Assert.NotEqual(first, second);
            Assert.True(hasher.Verify(plano, first));
            Assert.True(hasher.Verify(plano, second));
        }
    }
}
