using LogisticsSystem.Api.Common.Extensions;
using LogisticsSystem.Api.Contracts.Shipments;
using LogisticsSystem.Application.Authorization;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Dispatch.Queries.GetAssignmentHistory;
using LogisticsSystem.Application.Features.Shipments.Commands.AssignDriver;
using LogisticsSystem.Application.Features.Shipments.Commands.CancelShipment;
using LogisticsSystem.Application.Features.Shipments.Commands.CreateShipment;
using LogisticsSystem.Application.Features.Shipments.Commands.DeleteShipment;
using LogisticsSystem.Application.Features.Shipments.Commands.DeliverShipment;
using LogisticsSystem.Application.Features.Shipments.Commands.FailShipment;
using LogisticsSystem.Application.Features.Shipments.Commands.PickupShipment;
using LogisticsSystem.Application.Features.Shipments.Commands.StartTransit;
using LogisticsSystem.Application.Features.Shipments.Commands.UpdateShipment;
using LogisticsSystem.Application.Features.Shipments.Queries.GetAllShipments;
using LogisticsSystem.Application.Features.Shipments.Queries.GetMyShipments;
using LogisticsSystem.Application.Features.Shipments.Queries.GetShipmentById;
using LogisticsSystem.Application.Features.ShipmentStatusHistories.Queries.GetShipmentStatusHistory;
using LogisticsSystem.Application.Features.ShipmentTrackings.Commands.AddShipmentLocation;
using LogisticsSystem.Application.Features.ShipmentTrackings.DTOs;
using LogisticsSystem.Application.Features.ShipmentTrackings.Queries.GetLatestShipmentLocation;
using LogisticsSystem.Application.Features.ShipmentTrackings.Queries.GetShipmentTracking;
using LogisticsSystem.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogisticsSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShipmentsController : ControllerBase
    {
        private readonly ISender _sender;

        public ShipmentsController(ISender sender)
        {
            _sender = sender;
        }

        [Authorize(Policy = Policies.ShipmentCreate)]
        [HttpPost]
        public async Task<IActionResult> Create(CreateShipmentCommand command, CancellationToken cancellationToken)
        {
            var id = await _sender.Send(command, cancellationToken);

            var shipment = await _sender.Send(new GetShipmentByIdQuery(id), cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id }, shipment );
        }

        [Authorize(Roles = Roles.Customer)]
        [HttpGet("my-shipments")]
        public async Task<IActionResult> GetMyShipments(
            [FromQuery] GetMyShipmentsQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }

        [Authorize(Policy = Policies.ShipmentView)]
        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var shipment = await _sender.Send(new GetShipmentByIdQuery(id), cancellationToken);
            return Ok(shipment);
        }

        [Authorize(Policy = Policies.ShipmentViewAll)]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllShipmentsQuery query, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }

        [Authorize(Policy = Policies.ShipmentUpdate)]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateShipmentCommand command, CancellationToken cancellationToken)
        {
            if (id != command.Shipment.Id)
                return BadRequest("Route id does not match body id.");

            await _sender.Send(command, cancellationToken);

            return NoContent();
        }

        [Authorize(Policy = Policies.ShipmentDelete)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteShipmentCommand(id), cancellationToken);

            return NoContent();
        }


        [Authorize(Policy = Policies.DispatchAssignDriver)]
        [HttpPost("{id:guid}/assign-driver")]
        public async Task<IActionResult> AssignDriver(Guid id, AssignDriverRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new AssignDriverCommand(id, request.DriverId), cancellationToken);

            return NoContent();
        }


        [Authorize(Policy = Policies.DriverUpdateStatus)]
        [HttpPost("{id:guid}/pickup")]
        public async Task<IActionResult> Pickup(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new PickupShipmentCommand(id), cancellationToken);

            return NoContent();
        }

        [Authorize(Policy = Policies.DriverUpdateStatus)]
        [HttpPost("{id:guid}/start-transit")]
        public async Task<IActionResult> StartTransit(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new StartTransitCommand(id), cancellationToken);

            return NoContent();
        }

        [Authorize(Policy = Policies.DriverUpdateStatus)]
        [HttpPost("{id:guid}/deliver")]
        public async Task<IActionResult> Deliver(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeliverShipmentCommand(id), cancellationToken);

            return NoContent();
        }

        [Authorize(Policy = Policies.ShipmentCancel)]
        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new CancelShipmentCommand(id), cancellationToken);

            return NoContent();
        }

        [Authorize(Policy = Policies.DriverUpdateStatus)]
        [HttpPost("{id:guid}/fail")]
        public async Task<IActionResult> Fail(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new FailShipmentCommand(id), cancellationToken);

            return NoContent();
        }


        [Authorize(Policy = Policies.DriverUpdateStatus)]
        [EnableRateLimiting(RateLimiterPolicies.Tracking)]
        [HttpPost("{shipmentId:guid}/location")]
        public async Task<IActionResult> AddLocation(Guid shipmentId, AddShipmentLocationRequest request, CancellationToken cancellationToken)
        {
            var command = new AddShipmentLocationCommand(shipmentId, request.Latitude, request.Longitude);

            await _sender.Send(command, cancellationToken);

            return NoContent();
        }

        [Authorize(Policy = Policies.ShipmentView)]
        [EnableRateLimiting(RateLimiterPolicies.Tracking)]
        [HttpGet("{shipmentId:guid}/tracking")]
        public async Task<IActionResult> GetTracking(
            Guid shipmentId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var query = new GetShipmentTrackingQuery(
                shipmentId,
                pageNumber,
                pageSize);

            var result = await _sender.Send(query, cancellationToken);

            return Ok(result);
        }


        [HttpGet("{shipmentId:guid}/location/latest")]
        [Authorize(Policy = Policies.ShipmentView)]
        [EnableRateLimiting(RateLimiterPolicies.Tracking)]
        public async Task<ActionResult<ShipmentTrackingDto>> GetLatestLocation(
            Guid shipmentId,
            CancellationToken cancellationToken)
        {
            var query = new GetLatestShipmentLocationQuery(shipmentId);

            var result = await _sender.Send(query, cancellationToken);

            return Ok(result);
        }


        [HttpGet("{shipmentId:guid}/status-history")]
        [Authorize(Policy = Policies.ShipmentView)]
        public async Task<IActionResult> GetStatusHistory(Guid shipmentId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetShipmentStatusHistoryQuery(shipmentId), cancellationToken);

            return Ok(result);
        }

        [HttpGet("{shipmentId:guid}/assignments/history")]
        [Authorize(Policy = Policies.ShipmentView)]
        public async Task<ActionResult<PagedResult<AssignmentHistoryResponse>>> GetAssignmentHistory(
            Guid shipmentId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(
                new GetAssignmentHistoryQuery(
                    shipmentId,
                    pageNumber,
                    pageSize),
                cancellationToken);

            return Ok(result);
        }
    }
}
