namespace EcoTrack.Domain.Auth;

public interface IGerenciadorIdentidade
{
    Task<UsuarioDto> CadastrarAsync(
        Guid tenantId,
        string nome,
        string email,
        string senha,
        string perfil,
        CancellationToken cancellationToken
    );

    Task<CredenciaisUsuario?> AutenticarAsync(
        string email,
        string senha,
        CancellationToken cancellationToken
    );

    Task<UsuarioDto?> ObterPorIdAsync(Guid usuarioId, CancellationToken cancellationToken);
}

public interface IEmissorTokens
{
    TokenDto CriarToken(CredenciaisUsuario usuario);
}

public interface IRevogacaoToken
{
    Task RevogarAsync(string jti, DateTime expiracaoUtc, CancellationToken cancellationToken);
    Task<bool> EstaRevogadoAsync(string jti, CancellationToken cancellationToken);
}