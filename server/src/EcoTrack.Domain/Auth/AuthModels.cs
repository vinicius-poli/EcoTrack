namespace EcoTrack.Domain.Auth;

public sealed record UsuarioDto(
    Guid Id,
    Guid TenantId,
    string Nome,
    string Email,
    string Perfil,
    string? NomeOrganizacao = null
);

public sealed record CredenciaisUsuario(
    Guid Id,
    Guid TenantId,
    string Nome,
    string Email,
    string Perfil,
    string? NomeOrganizacao = null
);

public sealed record TokenDto(
    Guid UserId,
    Guid TenantId,
    string Nome,
    string Email,
    string Perfil,
    string AccessToken,
    DateTime DataExpiracaoEmUtc
);