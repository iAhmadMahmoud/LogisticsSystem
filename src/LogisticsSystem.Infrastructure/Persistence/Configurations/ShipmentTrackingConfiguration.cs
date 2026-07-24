using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsSystem.Infrastructure.Persistence.Configurations
{
    public class ShipmentTrackingConfiguration : BaseEntityConfiguration<ShipmentTracking>
    {
        public override void Configure(EntityTypeBuilder<ShipmentTracking> builder)
        {
            base.Configure(builder);

            builder.ToTable("ShipmentTrackings");

            builder.Property(x => x.Latitude)
                   .HasPrecision(9, 6);

            builder.Property(x => x.Longitude)
                   .HasPrecision(9, 6);

            builder.Property(x => x.RecordedAt)
                   .IsRequired();

            builder.HasIndex(x => x.ShipmentId);

            builder.HasIndex(x => x.RecordedAt);

            builder.HasOne(x => x.Shipment)
                   .WithMany(x => x.ShipmentTrackings)
                   .HasForeignKey(x => x.ShipmentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
