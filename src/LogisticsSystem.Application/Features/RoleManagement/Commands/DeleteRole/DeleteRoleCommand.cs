using MediatR;

namespace LogisticsSystem.Application.Features.RoleManagement.Commands.DeleteRole
{
    public sealed record DeleteRoleCommand(Guid Id) : IRequest;
}
