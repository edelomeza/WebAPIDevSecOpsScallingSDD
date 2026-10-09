using System;
using System.Collections.Generic;
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
    [Route("api/v{version:apiVersion}/ventas/pago")]
    [Authorize(Policy = "AdminPolicy")]
    public sealed class VentasPagoController : ControllerBase
    {
        private readonly IVentasPagoService _service;
        private readonly IValidator<PagoCreateDto> _createValidator;

        public VentasPagoController(IVentasPagoService service, IValidator<PagoCreateDto> createValidator)
        {
            _service = service;
            _createValidator = createValidator;
        }

        [HttpGet("pedido/{pedidoId:guid}")]
        public async Task<ActionResult<IReadOnlyList<PagoResponseDto>>> GetByPedidoId(Guid pedidoId, CancellationToken cancellationToken)
        {
            var dtos = await _service.GetByPedidoIdAsync(pedidoId, cancellationToken).ConfigureAwait(false);
            return Ok(dtos);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PagoResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var dto = await _service.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return dto is null ? throw new Services.NotFoundException($"Pago '{id}' no encontrado.") : Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<PagoResponseDto>> Create(PagoCreateDto dto, CancellationToken cancellationToken)
        {
            var failures = await ValidateAsync(_createValidator, dto, cancellationToken).ConfigureAwait(false);
            if (failures is not null)
            {
                return failures;
            }

            var created = await _service.CreateAsync(dto, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = created.id, version = "1" }, created);
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
