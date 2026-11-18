using EcoTrack.Application.Common;
using EcoTrack.Domain.Auth;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Auth;

public sealed record ObterUsuarioQuery(Guid UsuarioId) : IRequest<Result<UsuarioDto>>;

public sealed class ObterUsuarioQueryHandler(IGerenciadorIdentidade identidade)
    : IRequestHandler<ObterUsuarioQuery, Result<UsuarioDto>>
{
    public async Task<Result<UsuarioDto>> Handle(ObterUsuarioQuery query, CancellationToken ct)
    {
        var usuarioDomain = await identidade.ObterPorIdAsync(query.UsuarioId, ct);
        if (usuarioDomain is null)
        {
            return Result.Fail<UsuarioDto>(Erros.Criar("Usuário não encontrado.", "usuario"));
        }

        // Mapeando do DTO de Domínio para o DTO de Aplicação
        var usuarioDto = new UsuarioDto(
            usuarioDomain.Id,
            usuarioDomain.Nome,
            usuarioDomain.Email,
            usuarioDomain.Perfil,
            usuarioDomain.TenantId
        );

        return Result.Ok(usuarioDto);
    }
}