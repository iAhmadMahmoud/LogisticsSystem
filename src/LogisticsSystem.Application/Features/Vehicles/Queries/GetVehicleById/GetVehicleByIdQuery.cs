using LogisticsSystem.Application.Features.Vehicles.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Vehicles.Queries.GetVehicleById
{
    public sealed record GetVehicleByIdQuery(Guid Id) : IRequest<VehicleDto>;
}
