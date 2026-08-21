using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Dashboard.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Dashboard.Queries.GetRecentActivity
{
    public sealed record GetRecentActivityQuery(
        int PageNumber = 1,
        int PageSize = 10,
        string? ActivityType = null) : IRequest<PagedResult<RecentActivityDto>>;
}
