using System.Linq;
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
    [Route("api/v{version:apiVersion}/clientes")]
    [Authorize(Policy = "AdminPolicy")]
    public sealed class CliClienteController : ControllerBase
    {
        private readonly ICliClienteService _service;
        private readonly IValidator<CliClienteCreateDto> _createValidator;
        private readonly IValidator<CliClienteUpdateDto> _updateValidator;
        private readonly IValidator<CliClienteDeleteDto> _deleteValidator;

        public CliClienteController(
            ICliClienteService service,
            IValidator<CliClienteCreateDto> createValidator,
            IValidator<CliClienteUpdateDto> updateValidator,
            IValidator<CliClienteDeleteDto> deleteValidator)
        {
            _service = service;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _deleteValidator = deleteValidator;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<CliClienteDto>>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { error = "Page must be >= 1 and pageSize between 1 and 100." });
            }

            return Ok(await _service.GetPagedAsync(page, pageSize, cancellationToken).ConfigureAwait(false));
        }

        [HttpGet("search")]
        public async Task<ActionResult<PagedResult<CliClienteDto>>> SearchByName([FromQuery] string? texto, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return BadRequest(new { error = "El texto de búsqueda es requerido." });
            }

            if (texto.Trim().Length > 100)
            {
                return BadRequest(new { error = "El texto debe tener como máximo 100 caracteres." });
            }

            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { error = "Page must be >= 1 and pageSize between 1 and 100." });
            }

            return Ok(await _service.SearchByNameAsync(texto, page, pageSize, cancellationToken).ConfigureAwait(false));
        }

        [HttpGet("autocomplete")]
        public async Task<ActionResult<System.Collections.Generic.IReadOnlyList<CliClienteAutocompleteDto>>> Autocomplete([FromQuery] string? texto, [FromQuery] int maxResultados = 10, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return BadRequest(new { error = "El texto de búsqueda es requerido." });
            }

            if (texto.Trim().Length > 100)
            {
                return BadRequest(new { error = "El texto debe tener como máximo 100 caracteres." });
            }

            return Ok(await _service.AutocompleteAsync(texto, maxResultados, cancellationToken).ConfigureAwait(false));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CliClienteDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var dto = await _service.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<CliClienteDto>> Create(CliClienteCreateDto dto, CancellationToken cancellationToken)
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
        public async Task<ActionResult<CliClienteDto>> Update(int id, CliClienteUpdateDto dto, CancellationToken cancellationToken)
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

            try
            {
                var updated = await _service.UpdateAsync(id, dto, cancellationToken).ConfigureAwait(false);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (ConcurrencyConflictException)
            {
                return Conflict(new { error = "The record was modified by another process." });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id, [FromBody] CliClienteDeleteDto dto, CancellationToken cancellationToken)
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

            try
            {
                var deleted = await _service.DeleteAsync(id, dto, cancellationToken).ConfigureAwait(false);
                return deleted ? NoContent() : NotFound();
            }
            catch (ConcurrencyConflictException)
            {
                return Conflict(new { error = "The record was modified by another process." });
            }
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
