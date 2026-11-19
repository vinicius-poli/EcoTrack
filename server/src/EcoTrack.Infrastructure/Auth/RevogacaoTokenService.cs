using EcoTrack.Domain.Auth;
using EcoTrack.Infrastructure.Persistence;
using EcoTrack.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Infrastructure.Auth;

public sealed class RevogacaoTokenService(EcoTrackDbContext dbContext) : IRevogacaoToken
{
    public async Task RevogarAsync(string jti, DateTime expiracaoUtc, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jti))
            return;

        var agoraUtc = DateTime.UtcNow;

        // Se o token já expirou, não precisa nem gravar
        if (expiracaoUtc <= agoraUtc)
            return;

        var jaExiste = await dbContext.TokensRevogados
            .AnyAsync(x => x.Jti == jti, cancellationToken);

        if (jaExiste)
            return;

        dbContext.TokensRevogados.Add(new TokenRevogado
        {
            Id = Guid.CreateVersion7(),
            Jti = jti,
            ExpiracaoUtc = expiracaoUtc,
            RevogadoEmUtc = agoraUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EstaRevogadoAsync(string jti, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jti))
            return false;

        var agoraUtc = DateTime.UtcNow;

        return await dbContext.TokensRevogados
            .AsNoTracking()
            .AnyAsync(x => x.Jti == jti && x.ExpiracaoUtc > agoraUtc, cancellationToken);
    }
}