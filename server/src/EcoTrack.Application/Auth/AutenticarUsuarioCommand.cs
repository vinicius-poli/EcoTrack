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

        var usuario = await identidade.AutenticarAsync(command.Email, command.Senha, ct);
        if (usuario is null)
        {
            return Result.Fail<TokenDto>(Erros.Criar("Credenciais inválidas.", "Autenticação"));
        }

        // Gera o token passando as claims essenciais para o Multi-tenancy (UserId e TenantId)
        var token = emissorTokens.GerarToken(usuario.Id, usuario.Email, usuario.TenantId);

        return Result.Ok(new TokenDto(token));
    }
}