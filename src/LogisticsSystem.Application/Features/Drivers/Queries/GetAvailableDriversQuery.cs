using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Queries
{
    public sealed record GetAvailableDriversQuery : IRequest<IReadOnlyList<DriverResponse>>;
}
