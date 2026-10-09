using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;

namespace WebAPIDevSecOpsScallingSDD.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/two-factor")]
    [Authorize]
    public sealed class TwoFactorController : ControllerBase
    {
        private readonly ITwoFactorService _service;
        private readonly IValidator<TwoFactorVerifyRequest> _verifyValidator;

        public TwoFactorController(ITwoFactorService service, IValidator<TwoFactorVerifyRequest> verifyValidator)
        {
            _service = service;
            _verifyValidator = verifyValidator;
        }

        [HttpPost("setup")]
        public async Task<ActionResult<TwoFactorSetupResponse>> Setup(CancellationToken cancellationToken)
        {
            var userId = GetCallerUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Credenciales inválidas." });
            }

            var response = await _service.SetupAsync(userId, cancellationToken).ConfigureAwait(false);
            return response is null
                ? Unauthorized(new { error = "Credenciales inválidas." })
                : Ok(response);
        }

        [HttpPost("verify")]
        public async Task<ActionResult<TwoFactorVerifyResponse>> Verify(TwoFactorVerifyRequest request, CancellationToken cancellationToken)
        {
            var failures = await ValidateAsync(_verifyValidator, request, cancellationToken).ConfigureAwait(false);
            if (failures is not null)
            {
                return failures;
            }

            var userId = GetCallerUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Credenciales inválidas." });
            }

            var result = await _service.VerifyAsync(userId, request, cancellationToken).ConfigureAwait(false);
            if (result.Status == TwoFactorStatus.LockedOut)
            {
                return StatusCode(423, new { error = "Cuenta bloqueada temporalmente." });
            }

            if (result.Status == TwoFactorStatus.InvalidCode)
            {
                return Unauthorized(new { error = "Credenciales inválidas." });
            }

            return Ok(new TwoFactorVerifyResponse { Enabled = true });
        }

        private string? GetCallerUserId()
        {
            // NOTE (04-01): legacy sin JWT real; migrar a claim sub del JWT HS256.
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
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
