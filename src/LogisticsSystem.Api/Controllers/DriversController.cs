using LogisticsSystem.Api.Contracts.Drivers;
using LogisticsSystem.Application.Authorization;
using LogisticsSystem.Application.Features.Drivers.Commands.CreateDriver;
using LogisticsSystem.Application.Features.Drivers.Queries.GetAllDrivers;
using LogisticsSystem.Application.Features.Drivers.Queries.GetAvailableDrivers;
using LogisticsSystem.Application.Features.Drivers.Queries.GetDriverById;
using LogisticsSystem.Domain.Enums;
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



        [Authorize(Policy = Policies.DriverManage)]
        [HttpPost]
        public async Task<IActionResult> Create(CreateDriverRequest request, CancellationToken cancellationToken)
        {
            var command = new CreateDriverCommand(
                request.FirstName,
                request.LastName,
                request.Username,
                request.Email,
                request.Password,
                request.LicenseNumber);

            var driverId = await _sender.Send(command, cancellationToken);

            return Created($"/api/drivers/{driverId}", new { id = driverId });
            //return CreatedAtAction(nameof(GetById), new { id = driverId }, new { id = driverId });
        }

        [Authorize(Policy = Policies.DriverViewAll)]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DriverStatus? status = null,
            CancellationToken cancellationToken = default
            )
        {
            var query = new GetAllDriversQuery(pageNumber, pageSize, status);
            var result = await _sender.Send(query, cancellationToken);

            return Ok(result);

        }

        [Authorize(Policy = Policies.DriverView)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDriverById(Guid Id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetDriverByIdQuery(Id), cancellationToken);

            return Ok(result);
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