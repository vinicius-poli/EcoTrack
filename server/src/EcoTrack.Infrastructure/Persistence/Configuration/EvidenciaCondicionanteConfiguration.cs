using EcoTrack.Domain.Condicionantes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoTrack.Infrastructure.Persistence.Configurations;

public class EvidenciaCondicionanteConfiguration : IEntityTypeConfiguration<EvidenciaCondicionante>
{
    public void Configure(EntityTypeBuilder<EvidenciaCondicionante> builder)
    {
        builder.ToTable("TBEvidenciaCondicionante");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CondicionanteId)
            .IsRequired();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.EnviadoPorUserId)
            .IsRequired();

        builder.Property(e => e.NomeOriginalArquivo)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(e => e.IdentificadorArmazenamento)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(e => e.ContentType)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.TamanhoBytes)
            .IsRequired();

        builder.Property(e => e.NumeroArt)
            .HasMaxLength(100);

        builder.Property(e => e.Observacao)
            .HasMaxLength(1000);

        builder.Property(e => e.DataUploadUtc)
            .IsRequired();

        builder.HasIndex(e => e.CondicionanteId);
        builder.HasIndex(e => e.TenantId);
    }
}