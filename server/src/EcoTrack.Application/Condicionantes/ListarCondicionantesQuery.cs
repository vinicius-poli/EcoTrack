using EcoTrack.Application.Common;
using EcoTrack.Domain.Condicionantes;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Condicionantes;

public sealed record ListarCondicionantesQuery : IRequest<Result<List<CondicionanteDto>>>;

public sealed class ListarCondicionantesQueryHandler : IRequestHandler<ListarCondicionantesQuery, Result<List<CondicionanteDto>>>
{
    private readonly ICondicionanteRepository _condicionanteRepository;

    public ListarCondicionantesQueryHandler(ICondicionanteRepository condicionanteRepository)
    {
        _condicionanteRepository = condicionanteRepository;
    }

    public async Task<Result<List<CondicionanteDto>>> Handle(ListarCondicionantesQuery request, CancellationToken cancellationToken)
    {
        var condicionantes = await _condicionanteRepository.SelecionarTodosAsync(cancellationToken);

        var dtos = condicionantes.Select(DtoMapper.ToDto).ToList();

        return Result.Ok(dtos);
    }
}