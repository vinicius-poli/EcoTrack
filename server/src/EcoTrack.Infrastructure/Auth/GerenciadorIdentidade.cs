using EcoTrack.Domain.Auth;
using EcoTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Infrastructure.Auth;

public sealed class GerenciadorIdentidade(
    UserManager<EcoTrackUser> userManager,
    IOrganizacaoRepository organizacaoRepository)
    : IGerenciadorIdentidade
{
    public async Task<UsuarioDto> CadastrarAsync(
        Guid tenantId,
        string nome,
        string email,
        string senha,
        string perfil,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var emailLimpo = email.Trim();
        var perfilLimpo = string.IsNullOrWhiteSpace(perfil) ? "Usuario" : perfil.Trim();

        var user = new EcoTrackUser
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            UserName = emailLimpo,
            Email = emailLimpo,
            Nome = nome.Trim(),
            Perfil = perfilLimpo
        };

        var result = await userManager.CreateAsync(user, senha);

        if (result.Succeeded)
            return new UsuarioDto(user.Id, user.TenantId, user.Nome, user.Email!, user.Perfil);

        if (result.Errors.Any(x => x.Code is "DuplicateEmail" or "DuplicateUserName"))
            throw new InvalidOperationException("EMAIL_DUPLICADO");

        throw new ArgumentException(string.Join(" ", result.Errors.Select(x => x.Description)));
    }

    public async Task<CredenciaisUsuario?> AutenticarAsync(
        string email,
        string senha,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(email.Trim());

        if (user is null || await userManager.IsLockedOutAsync(user))
            return null;

        if (!await userManager.CheckPasswordAsync(user, senha))
        {
            await userManager.AccessFailedAsync(user);
            return null;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var organizacao = await organizacaoRepository.ObterPorIdAsync(user.TenantId, cancellationToken);

        return new CredenciaisUsuario(
            user.Id,
            user.TenantId,
            user.Nome,
            user.Email!,
            user.Perfil,
            organizacao?.Nome
        );
    }

    public async Task<UsuarioDto?> ObterPorIdAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.Users.FirstOrDefaultAsync(x => x.Id == usuarioId, cancellationToken);

        if (user is null)
            return null;

        return new UsuarioDto(user.Id, user.TenantId, user.Nome, user.Email!, user.Perfil);
    }
}