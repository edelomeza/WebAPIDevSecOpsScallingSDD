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
    [Route("api/v{version:apiVersion}/estados-venta")]
    [Authorize(Policy = "AdminPolicy")]
    [EnableRateLimiting(Services.RateLimitOptions.AdminPolicyName)]
    public sealed class VenCatEstadoController : ControllerBase
    {
        private readonly IVenCatEstadoService _service;
        private readonly IValidator<VenCatEstadoCreateDto> _createValidator;
        private readonly IValidator<VenCatEstadoUpdateDto> _updateValidator;
        private readonly IValidator<VenCatEstadoDeleteDto> _deleteValidator;

        public VenCatEstadoController(
            IVenCatEstadoService service,
            IValidator<VenCatEstadoCreateDto> createValidator,
            IValidator<VenCatEstadoUpdateDto> updateValidator,
            IValidator<VenCatEstadoDeleteDto> deleteValidator)
        {
            _service = service;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _deleteValidator = deleteValidator;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<VenCatEstadoDto>>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { error = "Page must be >= 1 and pageSize between 1 and 100." });
            }

            return Ok(await _service.GetPagedAsync(page, pageSize, cancellationToken).ConfigureAwait(false));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<VenCatEstadoDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var dto = await _service.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return dto is null ? throw new Services.NotFoundException($"Estado '{id}' no encontrado.") : Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<VenCatEstadoDto>> Create(VenCatEstadoCreateDto dto, CancellationToken cancellationToken)
        {
            var failures = await ValidateAsync(_createValidator, dto, cancellationToken).ConfigureAwait(false);
            if (failures is not null)
            {
                return failures;
            }

            var created = await _service.CreateAsync(dto, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = created.id, version = "1" }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<VenCatEstadoDto>> Update(int id, VenCatEstadoUpdateDto dto, CancellationToken cancellationToken)
        {
            if (dto.id != id)
            {
                return BadRequest(new { error = "Route id and body id must match." });
            }

            var failures = await ValidateAsync(_updateValidator, dto, cancellationToken).ConfigureAwait(false);
            if (failures is not null)
            {
                return failures;
            }

            var updated = await _service.UpdateAsync(id, dto, cancellationToken).ConfigureAwait(false);
            return updated is null ? throw new Services.NotFoundException($"Estado '{id}' no encontrado.") : Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id, [FromBody] VenCatEstadoDeleteDto dto, CancellationToken cancellationToken)
        {
            if (dto.id != id)
            {
                return BadRequest(new { error = "Route id and body id must match." });
            }

            var failures = await ValidateAsync(_deleteValidator, dto, cancellationToken).ConfigureAwait(false);
            if (failures is not null)
            {
                return failures;
            }

            var deleted = await _service.DeleteAsync(id, dto, cancellationToken).ConfigureAwait(false);
            return deleted ? NoContent() : throw new Services.NotFoundException($"Estado '{id}' no encontrado.");
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
