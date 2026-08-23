using LogisticsSystem.Api.Common.Extensions;
using LogisticsSystem.Api.Contracts.Users;
using LogisticsSystem.Application.Authorization;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Users.Commands.DeleteUser;
using LogisticsSystem.Application.Features.Users.Commands.UpdateUser;
using LogisticsSystem.Application.Features.Users.Commands.UpdateUserStatus;
using LogisticsSystem.Application.Features.Users.DTOs;
using LogisticsSystem.Application.Features.Users.Queries.GetUserById;
using LogisticsSystem.Application.Features.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogisticsSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting(RateLimiterPolicies.Admin)]
    public class UsersController : ControllerBase
    {
        private readonly ISender _sender;

        public UsersController(ISender sender)
        {
            _sender = sender;
        }

        [Authorize(Policy = Policies.UserView)]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? searchTerm = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUsersQuery(pageNumber, pageSize, role, isActive, searchTerm);
            var result = await _sender.Send(query, cancellationToken);

            return Ok(result);
        }

        [Authorize(Policy = Policies.UserView)]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetUserByIdQuery(id), cancellationToken);

            return Ok(result);
        }

        [Authorize(Policy = Policies.UserManage)]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(
            Guid id,
            [FromBody] UpdateUserRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateUserCommand(
                id,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.Email,
                request.UserName);

            var result = await _sender.Send(command, cancellationToken);

            return Ok(result);
        }

        [Authorize(Policy = Policies.UserManage)]
        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateStatus(
            Guid id,
            [FromBody] UpdateUserStatusRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateUserStatusCommand(id, request.IsActive);
            await _sender.Send(command, cancellationToken);

            return NoContent();
        }

        [Authorize(Policy = Policies.UserManage)]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteUserCommand(id), cancellationToken);

            return NoContent();
        }
    }
}
