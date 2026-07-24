using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.Identity;
using LogisticsSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsSystem.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : BaseEntityConfiguration<RefreshToken>
    {
        public override void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            base.Configure(builder);

            builder.ToTable("RefreshTokens");

            builder.Property(x=>x.Token)
                .HasMaxLength(500)
                .IsRequired();

            builder.HasIndex(x=>x.Token)
                .IsUnique();

            builder.HasIndex(x => x.UserId);

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.IsRevoked)
                .HasDefaultValue(false);

            builder.Property(x => x.ReplacedByToken)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.HasOne<ApplicationUser> ()
                .WithMany(x=>x.RefreshTokens)
                .HasForeignKey(x=>x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
