using System.Linq;
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
    [AllowAnonymous]
    [EnableRateLimiting(Services.RateLimitOptions.GlobalPolicyName)]
    public sealed class RefreshController : ControllerBase
    {
        private readonly IRefreshTokenService _service;
        private readonly IValidator<RefreshRequest> _validator;

        public RefreshController(IRefreshTokenService service, IValidator<RefreshRequest> validator)
        {
            _service = service;
            _validator = validator;
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<RefreshResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken)
        {
            var failures = await ValidateAsync(_validator, request, cancellationToken).ConfigureAwait(false);
            if (failures is not null)
            {
                return failures;
            }

            var result = await _service.RotateAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false);
            if (result.Status != RefreshStatus.Rotated || string.IsNullOrEmpty(result.Token) || string.IsNullOrEmpty(result.RefreshToken))
            {
                return Unauthorized(new { error = "Credenciales inválidas." });
            }

            return Ok(new RefreshResponse { Token = result.Token, RefreshToken = result.RefreshToken });
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
