using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Vehicles.DTOs;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Vehicles.Queries.GetAvailableVehicles
{
    public sealed record GetAvailableVehiclesQuery(
        int PageNumber = 1,
        int PageSize = 10,
        VehicleType? Type = null) : IRequest<PagedResult<VehicleDto>>;
}
