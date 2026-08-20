using LogisticsSystem.Api.Contracts.Drivers;
using LogisticsSystem.Application.Authorization;
using LogisticsSystem.Application.Features.Drivers.Commands.AssignVehicleToDriver;
using LogisticsSystem.Application.Features.Drivers.Commands.CreateDriver;
using LogisticsSystem.Application.Features.Drivers.Commands.UpdateDriverLocation;
using LogisticsSystem.Application.Features.Drivers.Commands.UpdateDriverStatus;
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

            //return Created($"/api/drivers/{driverId}", new { id = driverId });
            return CreatedAtAction(nameof(GetDriverById), new { id = driverId }, new { id = driverId });
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

        [Authorize(Policy =Policies.DriverUpdateStatus)]
        [HttpPatch("status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateDriverStatusCommand command,CancellationToken cancellationToken)
        {
            await _sender.Send(command, cancellationToken);
            return NoContent();
        }

        [Authorize(Policy = Policies.DriverUpdateStatus)]
        [HttpPatch("location")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateDriverLocationCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command, cancellationToken);

            return NoContent();
        }

        [Authorize(Policy = Policies.DriverManage)]
        [HttpPost("{driverId:guid}/vehicle")]
        public async Task<IActionResult> AssignVehicle(
            Guid driverId,
            [FromBody] AssignVehicleRequest request,
            CancellationToken cancellationToken)
        {
            var command = new AssignVehicleToDriverCommand(driverId, request.VehicleId);
            await _sender.Send(command, cancellationToken);

            return NoContent();
        }
    }
}