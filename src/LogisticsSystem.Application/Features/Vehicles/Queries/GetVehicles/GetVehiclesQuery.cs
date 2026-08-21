using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Vehicles.DTOs;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Vehicles.Queries.GetVehicles
{
    public sealed record GetVehiclesQuery(
        int PageNumber = 1,
        int PageSize = 10,
        VehicleType? Type = null,
        bool? IsActive = null,
        bool? IsAssigned = null,
        string? SearchTerm = null,
        string? SortBy = null,
        bool Descending = false) : IRequest<PagedResult<VehicleDto>>;
}
