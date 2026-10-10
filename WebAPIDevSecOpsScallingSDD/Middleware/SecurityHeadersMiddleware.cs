using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebAPIDevSecOpsScallingSDD.Middleware
{
    public sealed class SecurityHeadersMiddleware
    {
        public const string NonceItemKey = "CspNonce";

        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            ArgumentNullException.ThrowIfNull(next);
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            // NOTE (04-03): set sincrono como middleware mas externo: sobrevive al
            // WriteErrorAsync de ExceptionHandlingMiddleware (no limpia headers).
            // Sin OnStarting a proposito: DefaultHttpContext no dispara OnStarting
            // (solo servidor real) y seria codigo no testeable en Unit + mutantes
            // supervivientes en Stryker.
            ApplyStaticHeaders(context);
            if (!IsExempt(context.Request.Path))
            {
                var nonce = GenerateNonce();
                context.Items[NonceItemKey] = nonce;
                context.Response.Headers["Content-Security-Policy"] = BuildCsp(nonce);
            }

            await _next(context).ConfigureAwait(false);
        }

        private static void ApplyStaticHeaders(HttpContext context)
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["X-XSS-Protection"] = "0";
        }

        private static bool IsExempt(PathString path)
        {
            return path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase);
        }

        private static string GenerateNonce()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        }

        private static string BuildCsp(string nonce)
        {
            return $"default-src 'none'; script-src 'nonce-{nonce}'; style-src 'nonce-{nonce}'; frame-ancestors 'none'; base-uri 'none'; object-src 'none'; form-action 'none'";
        }
    }
}
