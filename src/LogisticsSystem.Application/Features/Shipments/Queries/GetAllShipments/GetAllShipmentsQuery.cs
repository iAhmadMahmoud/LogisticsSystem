using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Queries.GetAllShipments
{
    public sealed record GetAllShipmentsQuery(int PageNumber=1,int PageSize=10, string? SortBy = "CreatedAt", bool Descending = true) : IRequest<PagedResult<ShipmentDto>>;
}
