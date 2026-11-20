using EcoTrack.Domain.Condicionantes;
using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Domain.Licencas;

namespace EcoTrack.Application.Common;

public static partial class DtoMapper
{
    public static EmpreendimentoDto ToDto(Empreendimento entidade)
    {
        return new EmpreendimentoDto(
            entidade.Id,
            entidade.RazaoSocial,
            entidade.NomeFantasia,
            entidade.Cnpj,
            entidade.Municipio,
            entidade.Uf,
            entidade.Porte.ToString(),
            entidade.StatusLicenciamento.ToString(),
            entidade.CreatedAtUtc,
            entidade.UpdatedAtUtc
        );
    }
    public static LicencaDto ToDto(this Licenca licenca) =>
    new(
        licenca.Id,
        licenca.EmpreendimentoId,
        licenca.NumeroProcesso,
        licenca.NumeroLicenca,
        licenca.OrgaoEmissor,
        licenca.Tipo,
        licenca.DataEmissao,
        licenca.DataValidade,
        licenca.DiasAntecedenciaRenovacao,
        licenca.DataLimiteProtocoloRenovacao,
        licenca.Status,
        licenca.NumeroProtocoloRenovacao,
        licenca.DataProtocoloRenovacao,
        licenca.CreatedAtUtc,
        licenca.UpdatedAtUtc
    );

    public static EvidenciaCondicionanteDto ToDto(this EvidenciaCondicionante evidencia) =>
        new(
            evidencia.Id,
            evidencia.CondicionanteId,
            evidencia.NomeOriginalArquivo,
            evidencia.ContentType,
            evidencia.TamanhoBytes,
            evidencia.NumeroArt,
            evidencia.Observacao,
            evidencia.DataUploadUtc
        );

    public static CondicionanteDto ToDto(this Condicionante condicionante)
    {
        var evidenciasDto = condicionante.Evidencias
            .Select(e => e.ToDto())
            .ToList();

        return new CondicionanteDto(
            condicionante.Id,
            condicionante.LicencaId,
            condicionante.CodigoItem,
            condicionante.Titulo,
            condicionante.Descricao,
            condicionante.Periodicidade,
            condicionante.DataVencimento,
            condicionante.ResponsavelTecnico,
            condicionante.Status,
            condicionante.CalcularNivelRisco(),
            evidenciasDto.Count,
            condicionante.CreatedAtUtc,
            condicionante.UpdatedAtUtc,
            evidenciasDto
        );
    }

    
}