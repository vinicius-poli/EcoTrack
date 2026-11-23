using EcoTrack.Application.Common;
using EcoTrack.Domain.Auth;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Auth;

public sealed record CadastrarUsuarioCommand(
    string? Nome,
    string? Email,
    string? Senha,
    string? NomeOrganizacao)
    : IRequest<Result<UsuarioDto>>;

public sealed record UsuarioDto(
    Guid Id,
    Guid TenantId,
    string Nome,
    string Email,
    string Perfil,
    string? NomeOrganizacao = null);

public sealed class CadastrarUsuarioCommandHandler(
    IGerenciadorIdentidade identidade,
    IOrganizacaoRepository organizacaoRepository)
    : IRequestHandler<CadastrarUsuarioCommand, Result<UsuarioDto>>
{
    public async Task<Result<UsuarioDto>> Handle(CadastrarUsuarioCommand command, CancellationToken ct)
    {
        var erros = new List<IError>();

        if (string.IsNullOrWhiteSpace(command.Nome))
        {
            erros.Add(Erros.Criar("Nome é obrigatório.", "nome"));
        }

        if (string.IsNullOrWhiteSpace(command.NomeOrganizacao))
        {
            erros.Add(Erros.Criar("Nome da organização é obrigatório.", "nomeOrganizacao"));
        }

        if (string.IsNullOrWhiteSpace(command.Email)
            || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(command.Email))
        {
            erros.Add(Erros.Criar("Informe um e-mail válido.", "email"));
        }

        if (string.IsNullOrEmpty(command.Senha)
            || command.Senha.Length < 8
            || !command.Senha.Any(char.IsDigit)
            || !command.Senha.Any(caractere => !char.IsLetterOrDigit(caractere)))
        {
            erros.Add(Erros.Criar(
                "A senha deve ter pelo menos 8 caracteres, incluindo um número e um caractere especial.",
                "senha"));
        }

        if (erros.Count > 0)
        {
            return Result.Fail<UsuarioDto>(erros);
        }

        var tenantId = Guid.CreateVersion7();
        var nomeOrganizacaoLimpo = command.NomeOrganizacao!.Trim();
        const string perfilInicial = nameof(PerfilUsuario.Administrador);

        try
        {
            var organizacao = new Organizacao(tenantId, nomeOrganizacaoLimpo, DateTime.UtcNow);
            await organizacaoRepository.AdicionarAsync(organizacao, ct);

            var usuarioDomain = await identidade.CadastrarAsync(
                tenantId,
                command.Nome!.Trim(),
                command.Email!.Trim(),
                command.Senha!,
                perfilInicial,
                ct);

            var usuarioDto = new UsuarioDto(
                usuarioDomain.Id,
                usuarioDomain.TenantId,
                usuarioDomain.Nome,
                usuarioDomain.Email,
                usuarioDomain.Perfil,
                organizacao.Nome
            );

            return Result.Ok(usuarioDto);
        }
        catch (InvalidOperationException ex) when (ex.Message == "EMAIL_DUPLICADO")
        {
            return Result.Fail<UsuarioDto>(
                Erros.Criar("O e-mail já está cadastrado.", "email"));
        }
        catch (ArgumentException ex)
        {
            return Result.Fail<UsuarioDto>(Erros.Criar(ex.Message, "senha"));
        }
    }
}