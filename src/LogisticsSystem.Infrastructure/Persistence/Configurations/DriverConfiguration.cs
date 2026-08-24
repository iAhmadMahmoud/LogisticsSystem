using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.Identity;
using LogisticsSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsSystem.Infrastructure.Persistence.Configurations
{
    public class DriverConfiguration : AuditableEntityConfiguration<Driver>
    {
        public override void Configure(EntityTypeBuilder<Driver> builder)
        {
            base.Configure(builder);

            builder.ToTable("Drivers");

            builder.Property(x => x.LicenseNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(x => x.LicenseNumber)
                .IsUnique();

            builder.HasIndex(x => x.UserId)
               .IsUnique();

            builder.HasIndex(x => x.Status);

            builder.HasIndex(x => x.VehicleId);

            builder.Property(x => x.Status)
                .HasConversion<int>();

            builder.HasOne<ApplicationUser>()
               .WithOne(x => x.Driver)
               .HasForeignKey<Driver>(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Vehicle)
                .WithOne(x => x.Driver)
                .HasForeignKey<Driver>(x => x.VehicleId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Shipments)
                .WithOne(x => x.Driver)
                .HasForeignKey(x => x.DriverId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.DispatchAssignments)
                .WithOne(x => x.Driver)
                .HasForeignKey(x => x.DriverId);
        }
    }
}
