using EcoTrack.Application.Common;
using EcoTrack.Domain.Condicionantes;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Condicionantes;

public sealed record ListarCondicionantesPorLicencaQuery(Guid LicencaId) : IRequest<Result<List<CondicionanteDto>>>;

public sealed class ListarCondicionantesPorLicencaQueryHandler
    : IRequestHandler<ListarCondicionantesPorLicencaQuery, Result<List<CondicionanteDto>>>
{
    private readonly ICondicionanteRepository _condicionanteRepository;

    public ListarCondicionantesPorLicencaQueryHandler(ICondicionanteRepository condicionanteRepository)
    {
        _condicionanteRepository = condicionanteRepository;
    }

    public async Task<Result<List<CondicionanteDto>>> Handle(
        ListarCondicionantesPorLicencaQuery request,
        CancellationToken cancellationToken)
    {
        var condicionantes = await _condicionanteRepository.SelecionarPorLicencaIdAsync(
            request.LicencaId,
            cancellationToken);

       var dtos = condicionantes.Select(DtoMapper.ToDto).ToList();

        return Result.Ok(dtos);
    }
}