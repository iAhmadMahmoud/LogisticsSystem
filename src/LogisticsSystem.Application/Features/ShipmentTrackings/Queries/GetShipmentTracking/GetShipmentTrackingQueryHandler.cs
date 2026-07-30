using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.ShipmentTrackings.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.ShipmentTrackings.Queries.GetShipmentTracking
{
    public sealed class GetShipmentTrackingQueryHandler : IRequestHandler<GetShipmentTrackingQuery, PagedResult<ShipmentTrackingDto>>
    {
        public Task<PagedResult<ShipmentTrackingDto>> Handle(GetShipmentTrackingQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
