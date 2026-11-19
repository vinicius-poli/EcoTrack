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
}