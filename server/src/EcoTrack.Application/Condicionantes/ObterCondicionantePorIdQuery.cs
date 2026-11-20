using EcoTrack.Application.Common;
using EcoTrack.Domain.Condicionantes;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Condicionantes;

public sealed record ObterCondicionantePorIdQuery(Guid Id) : IRequest<Result<CondicionanteDto>>;

public sealed class ObterCondicionantePorIdQueryHandler
    : IRequestHandler<ObterCondicionantePorIdQuery, Result<CondicionanteDto>>
{
    private readonly ICondicionanteRepository _condicionanteRepository;

    public ObterCondicionantePorIdQueryHandler(ICondicionanteRepository condicionanteRepository)
    {
        _condicionanteRepository = condicionanteRepository;
    }

    public async Task<Result<CondicionanteDto>> Handle(
        ObterCondicionantePorIdQuery request,
        CancellationToken cancellationToken)
    {
        var condicionante = await _condicionanteRepository.SelecionarPorIdComEvidenciasAsync(
            request.Id,
            cancellationToken);

        if (condicionante is null)
        {
            return Result.Fail<CondicionanteDto>(Erros.Criar("Condicionante não encontrada.", "Id"));
        }

        return Result.Ok(condicionante.ToDto());
    }
}