using System;
using System.Collections.Generic;
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
    [Route("api/v{version:apiVersion}/ventas/detalles")]
    [Authorize]
    public sealed class VentaDetalleController : ControllerBase
    {
        private readonly IVentaDetalleService _service;
        private readonly IValidator<VenVentaDetalleCreateDto> _createValidator;
        private readonly IValidator<VenVentaDetalleDeleteDto> _deleteValidator;

        public VentaDetalleController(
            IVentaDetalleService service,
            IValidator<VenVentaDetalleCreateDto> createValidator,
            IValidator<VenVentaDetalleDeleteDto> deleteValidator)
        {
            _service = service;
            _createValidator = createValidator;
            _deleteValidator = deleteValidator;
        }

        [HttpGet("autocomplete-productos")]
        public async Task<ActionResult<IReadOnlyList<ProProductoAutocompleteDto>>> AutocompleteProductos([FromQuery] string? texto, [FromQuery] int maxResultados = 10, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return BadRequest(new { error = "El texto de búsqueda es requerido." });
            }

            if (texto.Trim().Length > 50)
            {
                return BadRequest(new { error = "El texto debe tener como máximo 50 caracteres." });
            }

            return Ok(await _service.AutocompleteProductoAsync(texto, maxResultados, cancellationToken).ConfigureAwait(false));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<VenVentaDetalleDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var dto = await _service.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost("~/api/v{version:apiVersion}/ventas/{idVenta:int}/detalles")]
        public async Task<ActionResult<VenVentaDetalleDto>> Create(int idVenta, VenVentaDetalleCreateDto dto, CancellationToken cancellationToken)
        {
            var failures = await ValidateAsync(_createValidator, dto, cancellationToken).ConfigureAwait(false);
            if (failures is not null)
            {
                return failures;
            }

            try
            {
                var created = await _service.AddDetalleAsync(idVenta, dto, GetCallerUserId(), cancellationToken).ConfigureAwait(false);
                return created is null ? NotFound() : CreatedAtAction(nameof(GetById), new { id = created.id, version = "1" }, created);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ValidationException ex)
            {
                return UnprocessableEntity(new { error = ex.Message });
            }
            catch (ConcurrencyConflictException)
            {
                return Conflict(new { error = "Stock insuficiente o el detalle fue modificado por otro proceso." });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id, [FromBody] VenVentaDetalleDeleteDto dto, CancellationToken cancellationToken)
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
                var deleted = await _service.RemoveDetalleAsync(id, dto, GetCallerUserId(), cancellationToken).ConfigureAwait(false);
                return deleted ? NoContent() : NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ConcurrencyConflictException)
            {
                return Conflict(new { error = "El detalle fue modificado por otro proceso." });
            }
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
