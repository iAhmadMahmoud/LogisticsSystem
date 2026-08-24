using LogisticsSystem.Api.Common.Extensions;
using LogisticsSystem.Api.Contracts.Roles;
using LogisticsSystem.Application.Authorization;
using LogisticsSystem.Application.Features.RoleManagement.Commands.AssignRoleToUser;
using LogisticsSystem.Application.Features.RoleManagement.Commands.CreateRole;
using LogisticsSystem.Application.Features.RoleManagement.Commands.DeleteRole;
using LogisticsSystem.Application.Features.RoleManagement.Commands.RemoveRoleFromUser;
using LogisticsSystem.Application.Features.RoleManagement.DTOs;
using LogisticsSystem.Application.Features.RoleManagement.Queries.GetRoles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogisticsSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Policies.UserManage)]
    [EnableRateLimiting(RateLimiterPolicies.Admin)]
    public class RolesController : ControllerBase
    {
        private readonly ISender _sender;

        public RolesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetRolesQuery(), cancellationToken);

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
        {
            var command = new CreateRoleCommand(request.Name);
            var result = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(nameof(GetRoles), new { id = result.Id }, result);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteRoleCommand(id), cancellationToken);

            return NoContent();
        }

        [HttpPost("users/{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignRole(
            Guid userId,
            [FromBody] AssignRoleRequest request,
            CancellationToken cancellationToken)
        {
            var command = new AssignRoleToUserCommand(userId, request.RoleName);
            await _sender.Send(command, cancellationToken);

            return NoContent();
        }

        [HttpDelete("users/{userId:guid}/{roleName}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> RemoveRole(
            Guid userId,
            string roleName,
            CancellationToken cancellationToken)
        {
            var command = new RemoveRoleFromUserCommand(userId, roleName);
            await _sender.Send(command, cancellationToken);

            return NoContent();
        }
    }
}
