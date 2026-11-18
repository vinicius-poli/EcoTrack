using EcoTrack.Domain.Empreendimentos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoTrack.Infrastructure.Persistence.Configurations;

public class EmpreendimentoConfiguration : IEntityTypeConfiguration<Empreendimento>
{
    public void Configure(EntityTypeBuilder<Empreendimento> builder)
    {
        builder.ToTable("TBEmpreendimento");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RazaoSocial)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.NomeFantasia)
            .HasMaxLength(150);

        builder.Property(e => e.Cnpj)
            .IsRequired()
            .HasMaxLength(14);

        builder.HasIndex(e => e.Cnpj)
            .IsUnique();

        builder.Property(e => e.Municipio)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Uf)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(e => e.Porte)
            .IsRequired();

        builder.Property(e => e.StatusLicenciamento)
            .IsRequired();
    }
}