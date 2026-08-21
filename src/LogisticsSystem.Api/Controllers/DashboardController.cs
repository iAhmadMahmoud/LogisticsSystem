using LogisticsSystem.Application.Authorization;
using LogisticsSystem.Application.Features.Dashboard.DTOs;
using LogisticsSystem.Application.Features.Dashboard.Queries.GetDriverDashboardMetrics;
using LogisticsSystem.Application.Features.Dashboard.Queries.GetShipmentDashboardMetrics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Policies.DashboardView)]
    public class DashboardController : ControllerBase
    {
        private readonly ISender _sender;

        public DashboardController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("shipments")]
        [ProducesResponseType(typeof(ShipmentDashboardMetricsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetShipmentMetrics(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetShipmentDashboardMetricsQuery(), cancellationToken);

            return Ok(result);
        }

        [HttpGet("drivers")]
        [ProducesResponseType(typeof(DriverDashboardMetricsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDriverMetrics(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetDriverDashboardMetricsQuery(), cancellationToken);

            return Ok(result);
        }
    }
}
