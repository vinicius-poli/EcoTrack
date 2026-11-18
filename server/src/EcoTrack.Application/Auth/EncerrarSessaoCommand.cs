using EcoTrack.Application.Common;
using EcoTrack.Domain.Auth; 
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Auth;

public sealed record EncerrarSessaoCommand(string Jti, DateTime ExpiraEmUtc) : IRequest<Result>;

public sealed class EncerrarSessaoCommandHandler(IRevogacaoToken revogacao)
    : IRequestHandler<EncerrarSessaoCommand, Result>
{
    public async Task<Result> Handle(EncerrarSessaoCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Jti))
        {
            return Result.Fail(Erros.Criar("O identificador do token (JTI) é obrigatório.", "jti"));
        }

        await revogacao.RevogarAsync(command.Jti, command.ExpiraEmUtc, ct);

        return Result.Ok();
    }
}