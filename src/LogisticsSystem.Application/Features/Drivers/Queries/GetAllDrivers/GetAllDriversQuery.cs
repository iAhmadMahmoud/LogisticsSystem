using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Queries.GetAllDrivers
{
    public sealed record GetAllDriversQuery(int PageNumber = 1, int PageSize = 10, DriverStatus? Status = null) : IRequest<PagedResult<DriverListItemResponse>>;


}
