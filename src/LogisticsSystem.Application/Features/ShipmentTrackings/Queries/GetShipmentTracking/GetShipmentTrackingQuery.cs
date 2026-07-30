using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.ShipmentTrackings.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogisticsSystem.Application.Features.ShipmentTrackings.Queries.GetShipmentTracking
{
    public sealed record GetShipmentTrackingQuery(Guid ShipmentId,int PageNumber= 1,int PageSize = 20) : IRequest<PagedResult<ShipmentTrackingDto>>;
       
}
