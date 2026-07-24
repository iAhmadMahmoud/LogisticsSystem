using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsSystem.Infrastructure.Persistence.Configurations
{
    public class VehicleConfiguration : AuditableEntityConfiguration<Vehicle>
    {
        public override void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            base.Configure(builder);

            builder.ToTable("Vehicles");

            builder.Property(x=>x.PlateNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(x => x.PlateNumber)
                .IsUnique();

            builder.Property(x=>x.Brand)
                .HasMaxLength(50);

            builder.Property(x=>x.Model)
                .HasMaxLength(50);

            builder.Property(x => x.Color)
              .HasMaxLength(30);

            builder.Property(x => x.Type)
                   .HasConversion<int>();

            builder.Property(x => x.Capacity)
                   .HasPrecision(10, 2);
        }
    }
}
