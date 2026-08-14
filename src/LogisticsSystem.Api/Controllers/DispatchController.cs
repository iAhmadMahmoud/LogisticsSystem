using LogisticsSystem.Application.Authorization;
using LogisticsSystem.Application.Features.Dispatch.Commands.AcceptDispatchAssignment;
using LogisticsSystem.Application.Features.Dispatch.Commands.RejectDispatchAssignment;
using LogisticsSystem.Application.Features.Dispatch.Queries.GetMyAssignments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
            await _sender.Send(new AcceptDispatchAssignmentCommand(assignmentId), cancellationToken);

            return NoContent();
        }

        [Authorize(Policy = Policies.DriverUpdateStatus)]
        [HttpPost("assignments/{assignmentId:guid}/reject")]
        public async Task<IActionResult> RejectAssignment(Guid assignmentId, CancellationToken cancellationToken)
        {
            await _sender.Send(new RejectDispatchAssignmentCommand(assignmentId), cancellationToken);

            return NoContent();
        }

        [Authorize(Policy =Policies.DriverUpdateStatus)]
        [HttpGet("my-assignments")]
        public async Task<IActionResult> GetMyAssignments([FromQuery] GetMyAssignmentsQuery query,CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query,cancellationToken);

            return Ok(result);
        }
    }
}
