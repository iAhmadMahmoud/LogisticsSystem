using AutoMapper;
using LogisticsSystem.Application.Features.Shipments.Commands.CreateShipment;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Shipments.Mapping
{
    public class ShipmentProfile : Profile
    {
        public ShipmentProfile()
        {
            CreateMap<Shipment,ShipmentDto>();

            CreateMap<CreateShipmentCommand, Shipment>();
        }
    }
}
