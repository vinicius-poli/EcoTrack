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