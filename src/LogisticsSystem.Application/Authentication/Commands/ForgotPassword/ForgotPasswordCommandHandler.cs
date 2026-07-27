using LogisticsSystem.Application.Common.Interfaces.Authentication;
using MediatR;

namespace LogisticsSystem.Application.Authentication.Commands.ForgotPassword
{
    public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
    {
        private readonly IIdentityService _identityService;

        public ForgotPasswordCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            await _identityService.ForgotPasswordAsync(request.Email);

            return Unit.Value;
        }
    }
}
