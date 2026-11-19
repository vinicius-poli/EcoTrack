using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoTrack.Infrastructure.Persistence.Configurations;

public sealed class TokenRevogadoConfiguration : IEntityTypeConfiguration<TokenRevogado>
{
    public void Configure(EntityTypeBuilder<TokenRevogado> builder)
    {
        builder.ToTable("TBTokensRevogados");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Jti).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.Jti).IsUnique();
        builder.HasIndex(x => x.ExpiracaoUtc);
    }
}