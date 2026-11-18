using EcoTrack.Domain.Empreendimentos;

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
}