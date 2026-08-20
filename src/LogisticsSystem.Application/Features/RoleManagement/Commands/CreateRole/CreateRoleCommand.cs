using LogisticsSystem.Application.Features.RoleManagement.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.RoleManagement.Commands.CreateRole
{
    public sealed record CreateRoleCommand(string Name) : IRequest<RoleDto>;
}
