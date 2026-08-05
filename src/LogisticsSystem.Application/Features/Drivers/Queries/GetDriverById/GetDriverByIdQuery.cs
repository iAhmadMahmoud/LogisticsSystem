using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Queries.GetDriverById
{
    public sealed record GetDriverByIdQuery(Guid DriverId) : IRequest<DriverDetailsResponse>;
}
