using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoTrack.Infrastructure.Persistence.Configurations;

public sealed class EcoTrackUserConfiguration : IEntityTypeConfiguration<EcoTrackUser>
{
    public void Configure(EntityTypeBuilder<EcoTrackUser> builder)
    {
        builder.Property(u => u.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Perfil)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.TenantId)
            .IsRequired();

        builder.HasIndex(u => u.TenantId);
    }
}