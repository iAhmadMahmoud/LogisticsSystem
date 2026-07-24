using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }


        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<Driver> Drivers => Set<Driver>();

        public DbSet<Vehicle> Vehicles => Set<Vehicle>();

        public DbSet<Shipment> Shipments => Set<Shipment>();

        public DbSet<ShipmentTracking> ShipmentTrackings => Set<ShipmentTracking>();

        public DbSet<Notification> Notifications => Set<Notification>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<DispatchAssignment> DispatchAssignments => Set<DispatchAssignment>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
