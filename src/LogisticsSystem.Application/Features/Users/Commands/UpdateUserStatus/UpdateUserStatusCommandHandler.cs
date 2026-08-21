using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Domain.Exceptions;
using MediatR;

namespace LogisticsSystem.Application.Features.Users.Commands.UpdateUserStatus
{
    public sealed class UpdateUserStatusCommandHandler : IRequestHandler<UpdateUserStatusCommand>
    {
        private readonly IIdentityService _identityService;
        private readonly ICurrentUserService _currentUserService;

        public UpdateUserStatusCommandHandler(
            IIdentityService identityService,
            ICurrentUserService currentUserService)
        {
            _identityService = identityService;
            _currentUserService = currentUserService;
        }

        public async Task Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
        {
            if (!request.IsActive && _currentUserService.UserId == request.Id)
            {
                throw new DomainException("Administrators cannot deactivate their own account.");
            }

            await _identityService.SetUserStatusAsync(request.Id, request.IsActive, cancellationToken);
        }
    }
}
