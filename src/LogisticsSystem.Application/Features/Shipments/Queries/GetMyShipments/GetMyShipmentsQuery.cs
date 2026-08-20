using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Queries.GetMyShipments
{
    public sealed record GetMyShipmentsQuery(
        int PageNumber = 1,
        int PageSize = 10,
        ShipmentStatus? Status = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        string? SortBy = "CreatedAt",
        bool Descending = true) : IRequest<PagedResult<ShipmentDto>>;
}
