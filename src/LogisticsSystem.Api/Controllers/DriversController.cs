using LogisticsSystem.Application.Authorization;
using LogisticsSystem.Application.Features.Drivers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriversController : ControllerBase
    {
        private readonly ISender _sender;

        public DriversController(ISender sender)
        {
            _sender = sender;
        }

        [Authorize(Policy = Policies.DispatchAssignDriver)]
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableDrivers(
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new GetAvailableDriversQuery(),
                cancellationToken);

            return Ok(result);
        }
    }
}
