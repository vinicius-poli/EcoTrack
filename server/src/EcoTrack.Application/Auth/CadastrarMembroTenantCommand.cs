using EcoTrack.Application.Common;
using EcoTrack.Domain.Auth;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Auth;

public sealed record CadastrarMembroRequest(
    string? Nome,
    string? Email,
    string? Senha,
    string? Perfil);

public sealed record CadastrarMembroTenantCommand(
    string? Nome,
    string? Email,
    string? Senha,
    string? Perfil)
    : IRequest<Result<UsuarioDto>>;

public sealed class CadastrarMembroTenantCommandHandler(
    IGerenciadorIdentidade identidade,
    IUserContext userContext)
    : IRequestHandler<CadastrarMembroTenantCommand, Result<UsuarioDto>>
{
    public async Task<Result<UsuarioDto>> Handle(CadastrarMembroTenantCommand command, CancellationToken ct)
    {
        if (userContext.TenantId is not Guid tenantId || tenantId == Guid.Empty)
        {
            return Result.Fail<UsuarioDto>(
                Erros.Criar("Tenant do usuário autenticado não foi identificado.", "tenantId"));
        }

        var erros = new List<IError>();

        if (string.IsNullOrWhiteSpace(command.Nome))
        {
            erros.Add(Erros.Criar("Nome é obrigatório.", "nome"));
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

        string? perfilNormalizado = null;

        if (string.IsNullOrWhiteSpace(command.Perfil)
            || !Enum.TryParse<PerfilUsuario>(command.Perfil.Trim(), ignoreCase: true, out var perfilEnum)
            || !Enum.IsDefined(perfilEnum)
            || perfilEnum is not (PerfilUsuario.Gestor or PerfilUsuario.Analista))
        {
            erros.Add(Erros.Criar("Informe um perfil válido: Gestor ou Analista.", "perfil"));
        }
        else
        {
            perfilNormalizado = perfilEnum.ToString();
        }

        if (erros.Count > 0)
        {
            return Result.Fail<UsuarioDto>(erros);
        }

        try
        {
            var usuarioCriado = await identidade.CadastrarAsync(
                tenantId,
                command.Nome!.Trim(),
                command.Email!.Trim(),
                command.Senha!,
                perfilNormalizado!,
                ct);

            return Result.Ok(new UsuarioDto(
                usuarioCriado.Id,
                usuarioCriado.Nome,
                usuarioCriado.Email,
                usuarioCriado.Perfil,
                usuarioCriado.TenantId));
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