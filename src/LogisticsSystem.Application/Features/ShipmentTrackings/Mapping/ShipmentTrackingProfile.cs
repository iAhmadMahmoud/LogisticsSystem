using AutoMapper;
using LogisticsSystem.Application.Features.ShipmentTrackings.DTOs;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.ShipmentTrackings.Mapping
{
    public class ShipmentTrackingProfile : Profile
    {
        public ShipmentTrackingProfile()
        {
            CreateMap<ShipmentTracking, ShipmentTrackingDto>();
        }
    }
}
