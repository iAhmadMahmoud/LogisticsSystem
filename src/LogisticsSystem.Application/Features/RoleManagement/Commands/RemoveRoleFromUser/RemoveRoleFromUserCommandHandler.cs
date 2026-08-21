using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Exceptions;
using MediatR;

namespace LogisticsSystem.Application.Features.RoleManagement.Commands.RemoveRoleFromUser
{
    public sealed class RemoveRoleFromUserCommandHandler : IRequestHandler<RemoveRoleFromUserCommand>
    {
        private readonly IIdentityService _identityService;
        private readonly ICurrentUserService _currentUserService;

        public RemoveRoleFromUserCommandHandler(
            IIdentityService identityService,
            ICurrentUserService currentUserService)
        {
            _identityService = identityService;
            _currentUserService = currentUserService;
        }

        public async Task Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId == request.UserId &&
                string.Equals(request.RoleName.Trim(), Roles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException("Administrators cannot remove the Admin role from their own account.");
            }

            await _identityService.RemoveRoleFromUserAsync(request.UserId, request.RoleName, cancellationToken);
        }
    }
}
