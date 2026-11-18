using EcoTrack.Application.Common;
using EcoTrack.Domain.Empreendimentos;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Empreendimentos;

public sealed record ExcluirEmpreendimentoCommand(Guid Id) : IRequest<Result>;

public sealed class ExcluirEmpreendimentoCommandHandler : IRequestHandler<ExcluirEmpreendimentoCommand, Result>
{
    private readonly IEmpreendimentoRepository _empreendimentoRepository;

    public ExcluirEmpreendimentoCommandHandler(IEmpreendimentoRepository empreendimentoRepository)
    {
        _empreendimentoRepository = empreendimentoRepository;
    }

    public async Task<Result> Handle(ExcluirEmpreendimentoCommand request, CancellationToken cancellationToken)
    {
        // 1. Verifica se o empreendimento existe antes de excluir
        var empreendimentoExistente = await _empreendimentoRepository.SelecionarPorIdAsync(request.Id, cancellationToken);
        if (empreendimentoExistente is null)
        {
            return Result.Fail(Erros.Criar("Empreendimento não encontrado.", "Id"));
        }

        // 2. Executa a exclusão no repositório
        var excluido = await _empreendimentoRepository.ExcluirAsync(request.Id, cancellationToken);
        if (!excluido)
        {
            return Result.Fail(Erros.Criar("Não foi possível excluir o empreendimento."));
        }

        return Result.Ok();
    }
}