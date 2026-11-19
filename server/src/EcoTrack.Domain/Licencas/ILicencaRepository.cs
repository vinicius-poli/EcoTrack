namespace EcoTrack.Domain.Licencas;

public interface ILicencaRepository
{
    Task CadastrarAsync(Licenca licenca, CancellationToken cancellationToken = default);
    Task<bool> EditarAsync(Guid idSelecionado, Licenca licencaAtualizada, CancellationToken cancellationToken = default);
    Task<bool> ExcluirAsync(Guid idSelecionado, CancellationToken cancellationToken = default);
    Task<Licenca?> SelecionarPorIdAsync(Guid idSelecionado, CancellationToken cancellationToken = default);
    Task<List<Licenca>> SelecionarTodosAsync(CancellationToken cancellationToken = default);
    Task<List<Licenca>> SelecionarPorEmpreendimentoIdAsync(Guid empreendimentoId, CancellationToken cancellationToken = default);
    Task<bool> ExisteNumeroLicencaAsync(string numeroLicenca, Guid? ignorarId = null, CancellationToken cancellationToken = default);
}