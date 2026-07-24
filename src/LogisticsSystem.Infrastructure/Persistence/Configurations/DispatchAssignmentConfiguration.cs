using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsSystem.Infrastructure.Persistence.Configurations
{
    public class DispatchAssignmentConfiguration : AuditableEntityConfiguration<DispatchAssignment>
    {
        public override void Configure(EntityTypeBuilder<DispatchAssignment> builder)
        {
            base.Configure(builder);

            builder.ToTable("DispatchAssignments");

            builder.Property(x => x.AttemptNumber)
                .IsRequired();

            builder.Property(x => x.Status)
               .HasConversion<int>()
               .HasDefaultValue(AssignmentStatus.Pending);

            builder.HasIndex(x => x.Status);

            builder.Property(x=>x.SentAt) 
                .IsRequired();

            builder.HasIndex(x => x.ShipmentId);

            builder.Property(x => x.DriverId);

            builder.HasIndex(x=> new
            {
                x.ShipmentId,
                x.DriverId,
                x.AttemptNumber
            }).IsUnique();

            builder.HasOne(x=>x.Shipment)
                .WithMany(x=>x.DispatchAssignments)
                .HasForeignKey(x=>x.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Driver)
               .WithMany(x => x.DispatchAssignments)
               .HasForeignKey(x => x.DriverId)
               .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
