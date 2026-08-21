using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Domain.Exceptions;
using MediatR;

namespace LogisticsSystem.Application.Features.Users.Commands.DeleteUser
{
    public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
    {
        private readonly IIdentityService _identityService;
        private readonly ICurrentUserService _currentUserService;

        public DeleteUserCommandHandler(
            IIdentityService identityService,
            ICurrentUserService currentUserService)
        {
            _identityService = identityService;
            _currentUserService = currentUserService;
        }

        public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId == request.Id)
            {
                throw new DomainException("Administrators cannot delete their own account.");
            }

            await _identityService.DeactivateOrDeleteUserAsync(request.Id, cancellationToken);
        }
    }
}
