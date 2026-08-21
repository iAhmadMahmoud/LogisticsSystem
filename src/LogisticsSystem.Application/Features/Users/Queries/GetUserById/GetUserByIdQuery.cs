using LogisticsSystem.Application.Features.Users.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Users.Queries.GetUserById
{
    public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserDetailsDto>;
}
