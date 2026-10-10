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
    [EnableRateLimiting(Services.RateLimitOptions.Login2faPolicyName)]
    public sealed class Login2FaController : ControllerBase
    {
        private readonly ILogin2FaService _service;
        private readonly IValidator<Login2FaVerifyRequest> _validator;

        public Login2FaController(ILogin2FaService service, IValidator<Login2FaVerifyRequest> validator)
        {
            _service = service;
            _validator = validator;
        }

        [HttpPost("login2fa/verify")]
        public async Task<ActionResult<Login2FaVerifyResponse>> Verify(Login2FaVerifyRequest request, CancellationToken cancellationToken)
        {
            var failures = await ValidateAsync(_validator, request, cancellationToken).ConfigureAwait(false);
            if (failures is not null)
            {
                return failures;
            }

            var result = await _service.VerifyAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.Status == Login2FaStatus.LockedOut)
            {
                return StatusCode(423, new { error = "Cuenta bloqueada temporalmente." });
            }

            if (result.Status == Login2FaStatus.InvalidCredentials || string.IsNullOrEmpty(result.Token))
            {
                return Unauthorized(new { error = "Credenciales inválidas." });
            }

            return Ok(new Login2FaVerifyResponse { Token = result.Token });
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
