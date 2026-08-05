using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Queries.GetAvailableDrivers
{
    public sealed record GetAvailableDriversQuery : IRequest<IReadOnlyList<DriverResponse>>;
}
