using EcoTrack.Application.Common;
using EcoTrack.Domain.Auth;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Auth;

public sealed record AutenticarUsuarioCommand(string? Email, string? Senha)
    : IRequest<Result<TokenDto>>;

public sealed record TokenDto(string Token);

public sealed class AutenticarUsuarioCommandHandler(
    IGerenciadorIdentidade identidade,
    IEmissorTokens emissorTokens)
    : IRequestHandler<AutenticarUsuarioCommand, Result<TokenDto>>
{
    public async Task<Result<TokenDto>> Handle(AutenticarUsuarioCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Senha))
        {
            return Result.Fail<TokenDto>(
                Erros.Criar("E-mail e senha são obrigatórios.", "credenciais"));
        }

        CredenciaisUsuario? credenciais = await identidade.AutenticarAsync(command.Email, command.Senha, ct);
        if (credenciais is null)
        {
            return Result.Fail<TokenDto>(Erros.Criar("Credenciais inválidas.", "Autenticação"));
        }

        Domain.Auth.TokenDto tokenGerado = emissorTokens.GerarToken(credenciais);

        return Result.Ok(new TokenDto(tokenGerado.AccessToken));
    }
}