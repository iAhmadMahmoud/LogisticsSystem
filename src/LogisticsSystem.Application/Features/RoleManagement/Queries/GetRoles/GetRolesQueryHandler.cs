using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.RoleManagement.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.RoleManagement.Queries.GetRoles
{
    public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
    {
        private readonly IIdentityService _identityService;

        public GetRolesQueryHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            return await _identityService.GetRolesAsync(cancellationToken);
        }
    }
}
