using LogisticsSystem.Application.Features.Shipments.Commands.CreateShipment;
using LogisticsSystem.Application.Features.Shipments.Commands.DeleteShipment;
using LogisticsSystem.Application.Features.Shipments.Commands.UpdateShipment;
using LogisticsSystem.Application.Features.Shipments.Queries.GetAllShipments;
using LogisticsSystem.Application.Features.Shipments.Queries.GetShipmentById;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

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

        [HttpPost]
        public async Task<IActionResult> Create(CreateShipmentCommand command, CancellationToken cancellationToken)
        {
            var id = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id }, null);
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetById(Guid id,CancellationToken cancellationToken)
        {
            var shipment = await _sender.Send(new GetShipmentByIdQuery(id), cancellationToken);
            return Ok(shipment);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllShipmentsQuery query, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateShipmentCommand command, CancellationToken cancellationToken)
        {
            if (id != command.Shipment.Id)
                return BadRequest("Route id does not match body id.");

            await _sender.Send(command, cancellationToken);

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteShipmentCommand(id), cancellationToken);

            return NoContent();
        }
    }
}
