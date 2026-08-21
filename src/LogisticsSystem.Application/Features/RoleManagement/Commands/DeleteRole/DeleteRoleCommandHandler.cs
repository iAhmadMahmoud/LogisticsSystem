using LogisticsSystem.Application.Common.Interfaces.Authentication;
using MediatR;

namespace LogisticsSystem.Application.Features.RoleManagement.Commands.DeleteRole
{
    public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand>
    {
        private readonly IIdentityService _identityService;

        public DeleteRoleCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            await _identityService.DeleteRoleAsync(request.Id, cancellationToken);
        }
    }
}
