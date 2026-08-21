using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Features.Users.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Users.Queries.GetUserById
{
    public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailsDto>
    {
        private readonly IIdentityService _identityService;

        public GetUserByIdQueryHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<UserDetailsDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var userDetails = await _identityService.GetUserDetailsByIdAsync(request.Id, cancellationToken);
            if (userDetails is null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            return userDetails;
        }
    }
}
