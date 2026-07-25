using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Models.Authentication;
using MediatR;

namespace LogisticsSystem.Application.Authentication.Commands.Commands.Register
{
    public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthenticationResult>
    {
        private readonly IIdentityService _identityService;

        public RegisterCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<AuthenticationResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            return await _identityService.RegisterAsync(request.Request);
        }
    }
}
