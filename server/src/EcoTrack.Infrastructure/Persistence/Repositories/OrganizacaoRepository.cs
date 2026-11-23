using EcoTrack.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Infrastructure.Persistence.Repositories;

public sealed class OrganizacaoRepository(EcoTrackDbContext dbContext) : IOrganizacaoRepository
{
    public async Task AdicionarAsync(Organizacao organizacao, CancellationToken ct)
    {
        await dbContext.Organizacoes.AddAsync(organizacao, ct);
        await dbContext.SaveChangesAsync(ct);
    }
    public Task<Organizacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
{
    return dbContext.Organizacoes
        .AsNoTracking()
        .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
}
}