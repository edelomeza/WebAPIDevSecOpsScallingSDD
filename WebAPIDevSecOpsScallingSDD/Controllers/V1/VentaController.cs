using System;
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
    [Route("api/v{version:apiVersion}/ventas")]
    [Authorize]
    public sealed class VentaController : ControllerBase
    {
        private readonly IVentaService _service;
        private readonly IValidator<VenVentaCreateDto> _createValidator;

        public VentaController(IVentaService service, IValidator<VenVentaCreateDto> createValidator)
        {
            _service = service;
            _createValidator = createValidator;
        }

        [HttpGet("search")]
        public async Task<ActionResult<PagedResult<VenVentaDto>>> Search([FromQuery] string? strClaveVenta, [FromQuery] string? strNombreCliente, [FromQuery] DateTime? dteFechaInicio, [FromQuery] DateTime? dteFechaFin, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            if ((strClaveVenta ?? string.Empty).Trim().Length > 10)
            {
                return BadRequest(new { error = "La clave debe tener como máximo 10 caracteres." });
            }

            if ((strNombreCliente ?? string.Empty).Trim().Length > 100)
            {
                return BadRequest(new { error = "El nombre debe tener como máximo 100 caracteres." });
            }

            if (dteFechaInicio.HasValue && dteFechaFin.HasValue && dteFechaInicio.Value > dteFechaFin.Value)
            {
                return BadRequest(new { error = "La fecha de inicio no puede ser mayor que la fecha de fin." });
            }

            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { error = "Page must be >= 1 and pageSize between 1 and 100." });
            }

            return Ok(await _service.SearchAsync(strClaveVenta, strNombreCliente, dteFechaInicio, dteFechaFin, page, pageSize, cancellationToken).ConfigureAwait(false));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<VenVentaDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var dto = await _service.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<VenVentaDto>> Create(VenVentaCreateDto dto, CancellationToken cancellationToken)
        {
            var failures = await ValidateAsync(_createValidator, dto, cancellationToken).ConfigureAwait(false);
            if (failures is not null)
            {
                return failures;
            }

            try
            {
                var created = await _service.CreateAsync(dto, cancellationToken).ConfigureAwait(false);
                return CreatedAtAction(nameof(GetById), new { id = created.id, version = "1" }, created);
            }
            catch (ValidationException ex)
            {
                return UnprocessableEntity(new { error = ex.Message });
            }
            catch (ConcurrencyConflictException)
            {
                return Conflict(new { error = "Stock insuficiente o la venta fue modificada por otro proceso." });
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
