using LogisticsSystem.Application.Features.Shipments.Commands.CreateShipment;
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
        public IActionResult GetById(Guid id)
        {
            return Ok();
        }
    }
}
