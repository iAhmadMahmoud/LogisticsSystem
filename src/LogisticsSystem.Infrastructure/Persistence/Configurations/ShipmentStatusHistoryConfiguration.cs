using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsSystem.Infrastructure.Persistence.Configurations
{
    public sealed class ShipmentStatusHistoryConfiguration : BaseEntityConfiguration<ShipmentStatusHistory>
    {
        public override void Configure(EntityTypeBuilder<ShipmentStatusHistory> builder)
        {
            base.Configure(builder);

            builder.ToTable("ShipmentStatusHistories");

            builder.Property(x => x.ShipmentId)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.ChangedAt)
                .IsRequired();

            builder.Property(x => x.ChangedByUserId)
                .IsRequired(false);

            builder.HasOne(x => x.Shipment)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new
            {
                x.ShipmentId,
                x.ChangedAt
            });
        }
    }
}
