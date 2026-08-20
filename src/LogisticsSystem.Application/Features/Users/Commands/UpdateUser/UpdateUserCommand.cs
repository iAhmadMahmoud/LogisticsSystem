using LogisticsSystem.Application.Features.Users.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Users.Commands.UpdateUser
{
    public sealed record UpdateUserCommand(
        Guid Id,
        string FirstName,
        string LastName,
        string? PhoneNumber,
        string Email,
        string UserName) : IRequest<UserDetailsDto>;
}
