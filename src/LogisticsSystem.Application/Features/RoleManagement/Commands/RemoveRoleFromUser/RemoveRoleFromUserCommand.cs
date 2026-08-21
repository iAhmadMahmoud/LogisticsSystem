using MediatR;

namespace LogisticsSystem.Application.Features.RoleManagement.Commands.RemoveRoleFromUser
{
    public sealed record RemoveRoleFromUserCommand(Guid UserId, string RoleName) : IRequest;
}
