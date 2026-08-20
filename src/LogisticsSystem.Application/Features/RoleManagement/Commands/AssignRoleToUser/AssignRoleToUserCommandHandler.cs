using LogisticsSystem.Application.Common.Interfaces.Authentication;
using MediatR;

namespace LogisticsSystem.Application.Features.RoleManagement.Commands.AssignRoleToUser
{
    public sealed class AssignRoleToUserCommandHandler : IRequestHandler<AssignRoleToUserCommand>
    {
        private readonly IIdentityService _identityService;

        public AssignRoleToUserCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
        {
            await _identityService.AssignRoleToUserAsync(request.UserId, request.RoleName, cancellationToken);
        }
    }
}
