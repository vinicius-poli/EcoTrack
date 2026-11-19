using EcoTrack.Application.Common;
using EcoTrack.Domain.Licencas;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Licencas;

public sealed record ListarLicencasQuery : IRequest<Result<List<LicencaDto>>>;

public sealed class ListarLicencasQueryHandler : IRequestHandler<ListarLicencasQuery, Result<List<LicencaDto>>>
{
    private readonly ILicencaRepository _licencaRepository;

    public ListarLicencasQueryHandler(ILicencaRepository licencaRepository)
    {
        _licencaRepository = licencaRepository;
    }

    public async Task<Result<List<LicencaDto>>> Handle(ListarLicencasQuery request, CancellationToken cancellationToken)
    {
        var licencas = await _licencaRepository.SelecionarTodosAsync(cancellationToken);

        var dtos = licencas.Select(DtoMapper.ToDto).ToList();

        return Result.Ok(dtos);
    }
}