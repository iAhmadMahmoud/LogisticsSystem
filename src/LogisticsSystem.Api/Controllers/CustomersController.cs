using LogisticsSystem.Api.Contracts.Customers;
using LogisticsSystem.Application.Features.Customers.Commands.UpdateCustomerProfile;
using LogisticsSystem.Application.Features.Customers.DTOs;
using LogisticsSystem.Application.Features.Customers.Queries.GetCustomerProfile;
using LogisticsSystem.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Customer)]
    public class CustomersController : ControllerBase
    {
        private readonly ISender _sender;

        public CustomersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("me")]
        public async Task<ActionResult<CustomerProfileDto>> GetMyProfile(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetCustomerProfileQuery(), cancellationToken);
            return Ok(result);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UpdateCustomerProfileRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateCustomerProfileCommand(
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.DefaultAddress);

            await _sender.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
