using EcoTrack.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoTrack.Infrastructure.Persistence.Configurations;

public sealed class OrganizacaoConfiguration : IEntityTypeConfiguration<Organizacao>
{
    public void Configure(EntityTypeBuilder<Organizacao> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(o => o.Nome);

        builder.Property(o => o.CriadoEmUtc)
            .IsRequired();
    }
}