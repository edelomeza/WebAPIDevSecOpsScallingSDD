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
    [Route("api/v{version:apiVersion}/ventas/dashboard")]
    [Authorize(Policy = "AdminPolicy")]
    public sealed class VentasDashboardController : ControllerBase
    {
        private readonly IVentasDashboardService _service;
        private readonly IValidator<DashboardFilterDto> _filterValidator;

        public VentasDashboardController(IVentasDashboardService service, IValidator<DashboardFilterDto> filterValidator)
        {
            _service = service;
            _filterValidator = filterValidator;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardDto>> Get(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] string? estadoSaga,
            CancellationToken cancellationToken)
        {
            var filter = new DashboardFilterDto { Desde = desde, Hasta = hasta, EstadoSaga = estadoSaga };
            var failures = await ValidateAsync(_filterValidator, filter, cancellationToken).ConfigureAwait(false);
            if (failures is not null)
            {
                return failures;
            }

            var dto = await _service.GetAsync(filter, cancellationToken).ConfigureAwait(false);
            return Ok(dto);
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
