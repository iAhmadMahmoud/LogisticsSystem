using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.RoleManagement.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.RoleManagement.Commands.CreateRole
{
    public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleDto>
    {
        private readonly IIdentityService _identityService;

        public CreateRoleCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            return await _identityService.CreateRoleAsync(request.Name, cancellationToken);
        }
    }
}
