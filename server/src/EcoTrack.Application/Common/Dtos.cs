using EcoTrack.Domain.Condicionantes;
using EcoTrack.Domain.Licencas;

namespace EcoTrack.Application.Common;

public sealed record EmpreendimentoDto(
    Guid Id,
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string Municipio,
    string Uf,
    string Porte,
    string StatusLicenciamento,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public sealed record LicencaDto(
    Guid Id,
    Guid EmpreendimentoId,
    string NumeroProcesso,
    string NumeroLicenca,
    string OrgaoEmissor,
    TipoLicenca Tipo,
    DateTime DataEmissao,
    DateTime DataValidade,
    int DiasAntecedenciaRenovacao,
    DateTime DataLimiteProtocoloRenovacao,
    StatusLicenca Status,
    string? NumeroProtocoloRenovacao,
    DateTime? DataProtocoloRenovacao,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public sealed record CondicionanteDto(
    Guid Id,
    Guid LicencaId,
    string CodigoItem,
    string Titulo,
    string Descricao,
    PeriodicidadeCondicionante Periodicidade,
    DateTime DataVencimento,
    string? ResponsavelTecnico,
    StatusCondicionante Status,
    NivelRiscoCondicionante NivelRisco,
    int QuantidadeEvidencias,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<EvidenciaCondicionanteDto> Evidencias
);

    public sealed record EvidenciaCondicionanteDto(
    Guid Id,
    Guid CondicionanteId,
    string NomeOriginalArquivo,
    string ContentType,
    long TamanhoBytes,
    string? NumeroArt,
    string? Observacao,
    DateTime DataUploadUtc
);