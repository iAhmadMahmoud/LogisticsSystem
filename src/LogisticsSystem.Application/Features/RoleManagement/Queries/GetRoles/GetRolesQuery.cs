using LogisticsSystem.Application.Features.RoleManagement.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.RoleManagement.Queries.GetRoles
{
    public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>;
}
