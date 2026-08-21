using MediatR;

namespace LogisticsSystem.Application.Features.Users.Commands.UpdateUserStatus
{
    public sealed record UpdateUserStatusCommand(Guid Id, bool IsActive) : IRequest;
}
