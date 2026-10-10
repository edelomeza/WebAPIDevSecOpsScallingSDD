using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;

namespace WebAPIDevSecOpsScallingSDD.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/auth")]
    [Authorize]
    [EnableRateLimiting(Services.RateLimitOptions.GlobalPolicyName)]
    public sealed class LogoutController : ControllerBase
    {
        private readonly IRefreshTokenService _service;
        private readonly IValidator<LogoutRequest> _validator;

        public LogoutController(IRefreshTokenService service, IValidator<LogoutRequest> validator)
        {
            _service = service;
            _validator = validator;
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
        {
            var failures = await ValidateAsync(_validator, request, cancellationToken).ConfigureAwait(false);
            if (failures is not null)
            {
                return failures;
            }

            // NOTE (04-01): con JWT real el jti siempre viene del claim; hoy fallback al hash del refresh.
            var jti = User.FindFirstValue("jti")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _service.LogoutAsync(request.RefreshToken, jti, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        private async Task<ActionResult?> ValidateAsync<T>(IValidator<T> validator, T dto, CancellationToken cancellationToken)
        {
            ValidationResult result = await validator.ValidateAsync(dto, cancellationToken).ConfigureAwait(false);
            if (result.IsValid)
            {
                return null;
            }

            foreach (var failure in result.Errors.DistinctBy(e => (e.PropertyName, e.ErrorMessage)))
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem();
        }
    }
}
