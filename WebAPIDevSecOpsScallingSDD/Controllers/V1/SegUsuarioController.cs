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
    [Route("api/v{version:apiVersion}/usuarios")]
    [Authorize(Policy = "AdminPolicy")]
    public sealed class SegUsuarioController : ControllerBase
    {
        private readonly ISegUsuarioService _service;
        private readonly IValidator<SegUsuarioCreateDto> _createValidator;
        private readonly IValidator<SegUsuarioUpdateDto> _updateValidator;
        private readonly IValidator<SegUsuarioDeleteDto> _deleteValidator;

        public SegUsuarioController(
            ISegUsuarioService service,
            IValidator<SegUsuarioCreateDto> createValidator,
            IValidator<SegUsuarioUpdateDto> updateValidator,
            IValidator<SegUsuarioDeleteDto> deleteValidator)
        {
            _service = service;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _deleteValidator = deleteValidator;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<SegUsuarioDto>>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { error = "Page must be >= 1 and pageSize between 1 and 100." });
            }

            return Ok(await _service.GetPagedAsync(page, pageSize, cancellationToken).ConfigureAwait(false));
        }

        [HttpGet("search")]
        public async Task<ActionResult<PagedResult<SegUsuarioDto>>> SearchByName([FromQuery] string? texto, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return BadRequest(new { error = "Texto is required." });
            }

            if (texto.Trim().Length > 50)
            {
                return BadRequest(new { error = "Texto must have at most 50 characters." });
            }

            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { error = "Page must be >= 1 and pageSize between 1 and 100." });
            }

            return Ok(await _service.SearchByNameAsync(texto, page, pageSize, cancellationToken).ConfigureAwait(false));
        }

        [HttpGet("autocomplete")]
        public async Task<ActionResult<System.Collections.Generic.IReadOnlyList<SegUsuarioAutocompleteDto>>> Autocomplete([FromQuery] string? texto, [FromQuery] int maxResultados = 10, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return BadRequest(new { error = "Texto is required." });
            }

            if (texto.Trim().Length > 50)
            {
                return BadRequest(new { error = "Texto must have at most 50 characters." });
            }

            return Ok(await _service.AutocompleteAsync(texto, maxResultados, cancellationToken).ConfigureAwait(false));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SegUsuarioDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var dto = await _service.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return dto is null ? throw new Services.NotFoundException($"Usuario '{id}' no encontrado.") : Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<SegUsuarioDto>> Create(SegUsuarioCreateDto dto, CancellationToken cancellationToken)
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
        public async Task<ActionResult<SegUsuarioDto>> Update(int id, SegUsuarioUpdateDto dto, CancellationToken cancellationToken)
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
            return updated is null ? throw new Services.NotFoundException($"Usuario '{id}' no encontrado.") : Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id, [FromBody] SegUsuarioDeleteDto dto, CancellationToken cancellationToken)
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
            return deleted ? NoContent() : throw new Services.NotFoundException($"Usuario '{id}' no encontrado.");
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
