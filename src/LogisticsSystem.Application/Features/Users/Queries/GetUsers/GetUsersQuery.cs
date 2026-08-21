using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Users.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Users.Queries.GetUsers
{
    public sealed record GetUsersQuery(
        int PageNumber = 1,
        int PageSize = 10,
        string? Role = null,
        bool? IsActive = null,
        string? SearchTerm = null) : IRequest<PagedResult<UserDto>>;
}
