using EcoTrack.Domain.Condicionantes;
using EcoTrack.Domain.Licencas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoTrack.Infrastructure.Persistence.Configurations;

public class CondicionanteConfiguration : IEntityTypeConfiguration<Condicionante>
{
    public void Configure(EntityTypeBuilder<Condicionante> builder)
    {
        builder.ToTable("TBCondicionante");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.LicencaId)
            .IsRequired();

        builder.Property(c => c.CodigoItem)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Titulo)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Descricao)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.Periodicidade)
            .IsRequired();

        builder.Property(c => c.DataVencimento)
            .IsRequired();

        builder.Property(c => c.ResponsavelTecnico)
            .HasMaxLength(150);

        builder.Property(c => c.Status)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .IsRequired();

        // Evidencias é exposta como IReadOnlyCollection<EvidenciaCondicionante> backing field _evidencias
        builder.HasMany(c => c.Evidencias)
            .WithOne(e => e.Condicionante)
            .HasForeignKey(e => e.CondicionanteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Evidencias)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Relacionamento com Licenca (RF03.1) — impede excluir licença com condicionantes vinculadas
        builder.HasOne(c => c.Licenca)
            .WithMany()
            .HasForeignKey(c => c.LicencaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices para consultas frequentes
        builder.HasIndex(c => c.LicencaId);
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.LicencaId, c.CodigoItem });
    }
}