namespace EcoTrack.Domain.Empreendimentos;

public interface IEmpreendimentoRepository
{
    Task CadastrarAsync(Empreendimento empreendimento, CancellationToken cancellationToken = default);
    Task<bool> EditarAsync(Guid idSelecionado, Empreendimento empreendimentoAtualizado, CancellationToken cancellationToken = default);
    Task<bool> ExcluirAsync(Guid idSelecionado, CancellationToken cancellationToken = default);
    Task<Empreendimento?> SelecionarPorIdAsync(Guid idSelecionado, CancellationToken cancellationToken = default);
    Task<List<Empreendimento>> SelecionarTodosAsync(CancellationToken cancellationToken = default);
    Task<List<Empreendimento>> FiltrarAsync(Predicate<Empreendimento> filtro, CancellationToken cancellationToken = default);
    Task<bool> ExisteCnpjAsync(string cnpjNormalizado, Guid? ignorarId = null, CancellationToken cancellationToken = default);
    Task<Empreendimento?> SelecionarPorCnpjAsync(string cnpjNormalizado, CancellationToken cancellationToken = default);
}