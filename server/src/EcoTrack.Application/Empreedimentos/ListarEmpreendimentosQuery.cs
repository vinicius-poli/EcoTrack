using EcoTrack.Application.Common;
using EcoTrack.Domain.Empreendimentos;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Empreendimentos;

public sealed record ListarEmpreendimentosQuery : IRequest<Result<List<EmpreendimentoDto>>>;

public sealed class ListarEmpreendimentosQueryHandler : IRequestHandler<ListarEmpreendimentosQuery, Result<List<EmpreendimentoDto>>>
{
    private readonly IEmpreendimentoRepository _empreendimentoRepository;

    public ListarEmpreendimentosQueryHandler(IEmpreendimentoRepository empreendimentoRepository)
    {
        _empreendimentoRepository = empreendimentoRepository;
    }

    public async Task<Result<List<EmpreendimentoDto>>> Handle(ListarEmpreendimentosQuery request, CancellationToken cancellationToken)
    {
        var empreendimentos = await _empreendimentoRepository.SelecionarTodosAsync(cancellationToken);

        var dtos = empreendimentos.Select(DtoMapper.ToDto).ToList();

        return Result.Ok(dtos);
    }
}