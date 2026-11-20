namespace EcoTrack.Domain.Condicionantes;

public interface ICondicionanteRepository
{
    Task CadastrarAsync(Condicionante condicionante, CancellationToken cancellationToken = default);
    Task<bool> EditarAsync(Condicionante condicionante, CancellationToken cancellationToken = default);
    Task<bool> ExcluirAsync(Guid idSelecionado, CancellationToken cancellationToken = default);

    Task<Condicionante?> SelecionarPorIdAsync(Guid idSelecionado, CancellationToken cancellationToken = default);
    Task<Condicionante?> SelecionarPorIdComEvidenciasAsync(Guid idSelecionado, CancellationToken cancellationToken = default);

    Task<List<Condicionante>> SelecionarTodosAsync(CancellationToken cancellationToken = default);
    Task<List<Condicionante>> SelecionarPorLicencaIdAsync(Guid licencaId, CancellationToken cancellationToken = default);
}