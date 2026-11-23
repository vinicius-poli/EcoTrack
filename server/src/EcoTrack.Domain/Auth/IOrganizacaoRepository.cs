namespace EcoTrack.Domain.Auth;

public interface IOrganizacaoRepository
{
    Task AdicionarAsync(Organizacao organizacao, CancellationToken ct);
    Task<Organizacao?> ObterPorIdAsync(Guid id, CancellationToken ct);
}