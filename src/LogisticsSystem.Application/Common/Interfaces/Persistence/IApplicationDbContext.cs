using LogisticsSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Common.Interfaces.Persistence
{
    public interface IApplicationDbContext
    {
        DbSet<Customer> Customers { get; }

        DbSet<Driver> Drivers { get; }

        DbSet<Vehicle> Vehicles { get; }

        DbSet<Shipment> Shipments { get; }

        DbSet<ShipmentTracking> ShipmentTrackings { get; }

        DbSet<Notification> Notifications { get; }

        DbSet<RefreshToken> RefreshTokens { get; }

        DbSet<DispatchAssignment> DispatchAssignments { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
