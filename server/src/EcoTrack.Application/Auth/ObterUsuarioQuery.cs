using EcoTrack.Application.Common;
using EcoTrack.Domain.Auth;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Auth;

public sealed record ObterUsuarioQuery(Guid UsuarioId) : IRequest<Result<UsuarioDto>>;

public sealed class ObterUsuarioQueryHandler(
    IGerenciadorIdentidade identidade,
    IOrganizacaoRepository organizacaoRepository)
    : IRequestHandler<ObterUsuarioQuery, Result<UsuarioDto>>
{
    public async Task<Result<UsuarioDto>> Handle(ObterUsuarioQuery query, CancellationToken ct)
    {
        var usuarioDomain = await identidade.ObterPorIdAsync(query.UsuarioId, ct);
        if (usuarioDomain is null)
        {
            return Result.Fail<UsuarioDto>(Erros.Criar("Usuário não encontrado.", "usuario"));
        }

        var organizacao = await organizacaoRepository.ObterPorIdAsync(usuarioDomain.TenantId, ct);

        // Mapeando do DTO de Domínio para o DTO de Aplicação
        var usuarioDto = new UsuarioDto(
            usuarioDomain.Id,
            usuarioDomain.TenantId,
            usuarioDomain.Nome,
            usuarioDomain.Email,
            usuarioDomain.Perfil,
            organizacao?.Nome
        );

        return Result.Ok(usuarioDto);
    }
}