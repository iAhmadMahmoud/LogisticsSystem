using LogisticsSystem.Application.Common.Interfaces.Authentication;
using MediatR;

namespace LogisticsSystem.Application.Authentication.Commands.Logout
{
    public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
    {
        private readonly IIdentityService? _identityService;

        public LogoutCommandHandler(IIdentityService? identityService)
        {
            _identityService = identityService;
        }

        public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            await _identityService.LogoutAsync(request.RefreshToken);

            return Unit.Value;
        }
    }
}
