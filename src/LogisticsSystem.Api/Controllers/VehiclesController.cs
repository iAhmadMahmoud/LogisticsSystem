using LogisticsSystem.Api.Contracts.Vehicles;
using LogisticsSystem.Application.Authorization;
using LogisticsSystem.Application.Features.Vehicles.Commands.CreateVehicle;
using LogisticsSystem.Application.Features.Vehicles.Commands.DeleteVehicle;
using LogisticsSystem.Application.Features.Vehicles.Commands.UpdateVehicle;
using LogisticsSystem.Application.Features.Vehicles.Queries.GetVehicleById;
using LogisticsSystem.Application.Features.Vehicles.Queries.GetVehicles;
using LogisticsSystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehiclesController : ControllerBase
    {
        private readonly ISender _sender;

        public VehiclesController(ISender sender)
        {
            _sender = sender;
        }

        [Authorize(Policy = Policies.VehicleManage)]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateVehicleRequest request,
            CancellationToken cancellationToken)
        {
            var command = new CreateVehicleCommand(
                request.PlateNumber,
                request.Brand,
                request.Model,
                request.ManufacturingYear,
                request.Color,
                request.Type,
                request.Capacity);

            var result = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [Authorize(Policy = Policies.VehicleView)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetVehicleByIdQuery(id), cancellationToken);
            return Ok(result);
        }

        [Authorize(Policy = Policies.VehicleViewAll)]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] VehicleType? type = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? isAssigned = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool descending = false,
            CancellationToken cancellationToken = default)
        {
            var query = new GetVehiclesQuery(
                PageNumber: pageNumber,
                PageSize: pageSize,
                Type: type,
                IsActive: isActive,
                IsAssigned: isAssigned,
                SearchTerm: searchTerm,
                SortBy: sortBy,
                Descending: descending);

            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }

        [Authorize(Policy = Policies.VehicleManage)]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateVehicleRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateVehicleCommand(
                id,
                request.PlateNumber,
                request.Brand,
                request.Model,
                request.ManufacturingYear,
                request.Color,
                request.Type,
                request.Capacity,
                request.IsActive);

            var result = await _sender.Send(command, cancellationToken);
            return Ok(result);
        }

        [Authorize(Policy = Policies.VehicleManage)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteVehicleCommand(id), cancellationToken);
            return NoContent();
        }
    }
}
