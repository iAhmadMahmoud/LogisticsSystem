using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.Users.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Users.Commands.UpdateUser
{
    public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDetailsDto>
    {
        private readonly IIdentityService _identityService;

        public UpdateUserCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<UserDetailsDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            return await _identityService.UpdateUserByAdminAsync(
                request.Id,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.Email,
                request.UserName,
                cancellationToken);
        }
    }
}
