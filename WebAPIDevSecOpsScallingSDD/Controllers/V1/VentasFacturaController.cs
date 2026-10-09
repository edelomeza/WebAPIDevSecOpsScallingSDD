using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;

namespace WebAPIDevSecOpsScallingSDD.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/ventas/factura")]
    [Authorize(Policy = "AdminPolicy")]
    public sealed class VentasFacturaController : ControllerBase
    {
        private readonly IVentasFacturaService _service;

        public VentasFacturaController(IVentasFacturaService service)
        {
            _service = service;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<VenPedidoFacturaResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var dto = await _service.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return dto is null ? throw new Services.NotFoundException($"Factura '{id}' no encontrada.") : Ok(dto);
        }
    }
}
