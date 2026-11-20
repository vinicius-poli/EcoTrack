using EcoTrack.Application.Common;
using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Domain.Licencas;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Empreendimentos;

public sealed record ExcluirEmpreendimentoCommand(Guid Id) : IRequest<Result>;

public sealed class ExcluirEmpreendimentoCommandHandler : IRequestHandler<ExcluirEmpreendimentoCommand, Result>
{
    private readonly IEmpreendimentoRepository _empreendimentoRepository;
    private readonly ILicencaRepository _licencaRepository;

    public ExcluirEmpreendimentoCommandHandler(
        IEmpreendimentoRepository empreendimentoRepository,
        ILicencaRepository licencaRepository)
    {
        _empreendimentoRepository = empreendimentoRepository;
        _licencaRepository = licencaRepository;
    }

    public async Task<Result> Handle(ExcluirEmpreendimentoCommand request, CancellationToken cancellationToken)
    {
        // 1. Verifica se o empreendimento existe antes de excluir
        var empreendimentoExistente = await _empreendimentoRepository.SelecionarPorIdAsync(request.Id, cancellationToken);
        if (empreendimentoExistente is null)
        {
            return Result.Fail(Erros.Criar("Empreendimento não encontrado.", "Id"));
        }

        // 2. Verifica se há licenças ambientais vinculadas ao empreendimento
        var licencasVinculadas = await _licencaRepository
            .SelecionarPorEmpreendimentoIdAsync(request.Id, cancellationToken);

        if (licencasVinculadas.Count > 0)
        {
            return Result.Fail(Erros.Criar(
                "Não é possível excluir o empreendimento pois existem licenças ambientais vinculadas a ele.",
                "Id"));
        }

        // 3. Executa a exclusão no repositório
        var excluido = await _empreendimentoRepository.ExcluirAsync(request.Id, cancellationToken);
        if (!excluido)
        {
            return Result.Fail(Erros.Criar("Não foi possível excluir o empreendimento."));
        }

        return Result.Ok();
    }
}