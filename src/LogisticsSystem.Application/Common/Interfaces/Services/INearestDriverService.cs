using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Common.Interfaces.Services
{
    public interface INearestDriverService
    {
        Task<NearestDriverResult?> FindNerstAsync(Shipment shipment, CancellationToken cancellationToken=default);
    }
}
