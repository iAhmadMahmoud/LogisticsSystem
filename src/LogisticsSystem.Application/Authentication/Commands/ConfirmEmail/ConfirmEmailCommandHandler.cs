using LogisticsSystem.Application.Common.Interfaces.Authentication;
using MediatR;

namespace LogisticsSystem.Application.Authentication.Commands.ConfirmEmail
{
    public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Unit>
    {
        private readonly IIdentityService _identityService;

        public ConfirmEmailCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<Unit> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            await _identityService.ConfirmEmailAsync(request.UserId.ToString(), request.Token);
            
            return Unit.Value;
        }
    }
}
