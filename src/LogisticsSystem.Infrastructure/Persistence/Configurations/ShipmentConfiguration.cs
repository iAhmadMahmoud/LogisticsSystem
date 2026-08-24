using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsSystem.Infrastructure.Persistence.Configurations
{
    public class ShipmentConfiguration : AuditableEntityConfiguration<Shipment>
    {
        public override void Configure(EntityTypeBuilder<Shipment> builder)
        {
            base.Configure(builder);

            builder.ToTable("Shipments");

            builder.Property(x=>x.TrackingNumber)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(x => x.TrackingNumber)
                .IsUnique();

            builder.HasIndex(x => x.Status);

            builder.HasIndex(x => x.CustomerId);

            builder.HasIndex(x => x.DriverId);

            builder.HasIndex(x => new { x.CustomerId, x.CreatedAt });

            builder.HasIndex(x => new { x.Status, x.CreatedAt });

            builder.HasIndex(x => x.CreatedAt);

            builder.HasIndex(x => x.ScheduledAt);

            builder.Property(x => x.PickupAddress)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.DeliveryAddress)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Weight)
                .HasPrecision(10, 2);

            builder.Property(x => x.DistanceKm)
                .HasPrecision(10, 2);

            builder.Property(x=>x.ShippingCost)
                .HasPrecision(10, 2);

            builder.Property(x => x.Priority)
                .HasConversion<int>();

            builder.Property(x => x.Status)
                .HasConversion<int>();

            builder.Property(x => x.Notes)
                .HasMaxLength(1000);


            // Coordinates

            builder.Property(x => x.PickupLatitude)
                .HasPrecision(9, 6);

            builder.Property(x => x.PickupLongitude)
                .HasPrecision(9, 6);

            builder.Property(x => x.DeliveryLatitude)
                .HasPrecision(9, 6);

            builder.Property(x => x.DeliveryLongitude)
                .HasPrecision(9, 6);

            // Relationships

            builder.HasOne(x => x.Customer)
               .WithMany(x => x.Shipments)
               .HasForeignKey(x => x.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Driver)
                .WithMany(x => x.Shipments)
                .HasForeignKey(x => x.DriverId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.ShipmentTrackings)
                .WithOne(x => x.Shipment)
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.DispatchAssignments)
                .WithOne(x => x.Shipment)
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);



        }
    }
}
