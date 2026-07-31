using LogisticsSystem.Application.Authorization;
using LogisticsSystem.Application.Features.Dispatch.Commands.AcceptDispatchAssignment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DispatchController : ControllerBase
    {
        private readonly ISender _sender;

        public DispatchController(ISender sender)
        {
            _sender = sender;
        }


        [Authorize(Policy = Policies.DriverUpdateStatus)]
        [HttpPost("assignments/{assignmentId:guid}/accept")]
        public async Task<IActionResult> AcceptAssignment(
            Guid assignmentId,
            CancellationToken cancellationToken)
        {
            await _sender.Send( new AcceptDispatchAssignmentCommand(assignmentId), cancellationToken);

            return NoContent();
        }
    }
}
