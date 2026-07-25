using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Queries.GetAllShipments
{
    public sealed record GetAllShipmentsQuery(int PageNumber=1,int PageSize=10, string? SortBy = "CreatedAt", bool Descending = true, ShipmentStatus? Status = null, ShipmentPriority? Priority = null, Guid? CustomerId = null, Guid? DriverId = null, DateTime? ScheduledFrom = null, 
      DateTime? ScheduledTo = null, string? Search = null) : IRequest<PagedResult<ShipmentDto>>;
}
