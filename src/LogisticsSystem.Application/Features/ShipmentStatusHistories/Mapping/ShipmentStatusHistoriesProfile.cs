using AutoMapper;
using LogisticsSystem.Application.Features.ShipmentStatusHistories.DTOs;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.ShipmentStatusHistories.Mapping
{
    public class ShipmentStatusHistoriesProfile : Profile
    {
        public ShipmentStatusHistoriesProfile()
        {
            CreateMap<ShipmentStatusHistory,ShipmentStatusHistoryDto>();
        }
    }
}
