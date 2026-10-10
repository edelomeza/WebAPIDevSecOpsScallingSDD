using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebAPIDevSecOpsScallingSDD.Middleware;

namespace UnitTest.Middleware
{
    public class SecurityHeadersTests
    {
        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new SecurityHeadersMiddleware(null!));
        }

        [Fact]
        public async Task NullContextThrowsArgumentNull()
        {
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.InvokeAsync(null!));
        }

        [Fact]
        public async Task StandardRequestEmitsAllHeadersWithValidNonce()
        {
            var context = CreateContext("/api/v1/clientes");
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(context);

            AssertStaticHeaders(context);
            var csp = Assert.Single(context.Response.Headers, h => h.Key == "Content-Security-Policy").Value.ToString();
            var nonce = Assert.IsType<string>(context.Items[SecurityHeadersMiddleware.NonceItemKey]);
            Assert.False(string.IsNullOrWhiteSpace(nonce));
            var nonceBytes = Convert.FromBase64String(nonce);
            Assert.Equal(16, nonceBytes.Length);
            Assert.Contains($"script-src 'nonce-{nonce}'", csp, StringComparison.Ordinal);
            Assert.Contains($"style-src 'nonce-{nonce}'", csp, StringComparison.Ordinal);
            Assert.Contains("default-src 'none'", csp, StringComparison.Ordinal);
            Assert.Contains("frame-ancestors 'none'", csp, StringComparison.Ordinal);
        }

        [Fact]
        public async Task NonceIsUniquePerRequest()
        {
            var first = CreateContext("/api/v1/clientes");
            var second = CreateContext("/api/v1/clientes");
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(first);
            await middleware.InvokeAsync(second);

            var firstNonce = Assert.IsType<string>(first.Items[SecurityHeadersMiddleware.NonceItemKey]);
            var secondNonce = Assert.IsType<string>(second.Items[SecurityHeadersMiddleware.NonceItemKey]);
            Assert.NotEqual(firstNonce, secondNonce);
        }

        [Theory]
        [InlineData("/scalar/v1")]
        [InlineData("/scalar")]
        [InlineData("/openapi/v1.json")]
        [InlineData("/openapi")]
        [InlineData("/SCALAR/v1")]
        public async Task ExemptPathsOmitCspButKeepStaticHeaders(string path)
        {
            var context = CreateContext(path);
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(context);

            AssertStaticHeaders(context);
            Assert.DoesNotContain(context.Response.Headers, h => h.Key == "Content-Security-Policy");
            Assert.False(context.Items.ContainsKey(SecurityHeadersMiddleware.NonceItemKey));
        }

        [Fact]
        public async Task HeadersAreSetBeforeCallingNext()
        {
            var context = CreateContext("/api/v1/clientes");
            var observed = false;
            var middleware = new SecurityHeadersMiddleware(inner =>
            {
                // El set ocurre antes del pipeline interno: asi sobrevive al
                // WriteErrorAsync de 403/500 (verificado end-to-end en HeaderTests).
                AssertStaticHeaders(inner);
                Assert.True(inner.Response.Headers.ContainsKey("Content-Security-Policy"));
                observed = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.True(observed);
        }

        private static void AssertStaticHeaders(HttpContext context)
        {
            Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
            Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
            Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"].ToString());
            Assert.Equal("0", context.Response.Headers["X-XSS-Protection"].ToString());
        }

        private static DefaultHttpContext CreateContext(string path)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = new PathString(path);
            context.Response.Body = new MemoryStream();
            return context;
        }
    }
}
