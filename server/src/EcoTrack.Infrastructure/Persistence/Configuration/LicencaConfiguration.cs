using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Domain.Licencas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoTrack.Infrastructure.Persistence.Configurations;

public class LicencaConfiguration : IEntityTypeConfiguration<Licenca>
{
    public void Configure(EntityTypeBuilder<Licenca> builder)
    {
        builder.ToTable("TBLicenca");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.EmpreendimentoId)
            .IsRequired();

        builder.Property(l => l.NumeroProcesso)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.NumeroLicenca)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.OrgaoEmissor)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(l => l.Tipo)
            .IsRequired();

        builder.Property(l => l.DataEmissao)
            .IsRequired();

        builder.Property(l => l.DataValidade)
            .IsRequired();

        builder.Property(l => l.DiasAntecedenciaRenovacao)
            .IsRequired();

        builder.Property(l => l.Status)
            .IsRequired();

        builder.Property(l => l.NumeroProtocoloRenovacao)
            .HasMaxLength(100);

        builder.Property(l => l.DataProtocoloRenovacao);

        // DataLimiteProtocoloRenovacao é calculada no domínio (RF02.3), sem coluna física
        builder.Ignore(l => l.DataLimiteProtocoloRenovacao);

        // Relacionamento com Empreendimento (RF02.1)
        builder.HasOne(l => l.Empreendimento)
            .WithMany()
            .HasForeignKey(l => l.EmpreendimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice para consultas/verificação de duplicidade por NumeroLicenca
        builder.HasIndex(l => l.NumeroLicenca);

        // Índice para buscar licenças de um empreendimento
        builder.HasIndex(l => l.EmpreendimentoId);
    }
}