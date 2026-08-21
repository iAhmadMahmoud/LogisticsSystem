using MediatR;

namespace LogisticsSystem.Application.Features.RoleManagement.Commands.AssignRoleToUser
{
    public sealed record AssignRoleToUserCommand(Guid UserId, string RoleName) : IRequest;
}
