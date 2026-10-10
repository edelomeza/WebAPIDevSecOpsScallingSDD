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
    [EnableRateLimiting(Services.RateLimitOptions.LoginPolicyName)]
    public sealed class LoginController : ControllerBase
    {
        private readonly ILoginService _service;
        private readonly IValidator<LoginRequest> _validator;

        public LoginController(ILoginService service, IValidator<LoginRequest> validator)
        {
            _service = service;
            _validator = validator;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            var failures = await ValidateAsync(_validator, request, cancellationToken).ConfigureAwait(false);
            if (failures is not null)
            {
                return failures;
            }

            var result = await _service.AuthenticateAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.Status == LoginStatus.LockedOut)
            {
                return StatusCode(423, new { error = "Cuenta bloqueada temporalmente." });
            }

            if (result.Status == LoginStatus.RequiresTwoFactor)
            {
                return Ok(new LoginResponse { Requires2fa = true, TempToken = result.TempCode });
            }

            if (result.Status == LoginStatus.InvalidCredentials || string.IsNullOrEmpty(result.Token))
            {
                return Unauthorized(new { error = "Credenciales inválidas." });
            }

            return Ok(new LoginResponse { Token = result.Token });
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
