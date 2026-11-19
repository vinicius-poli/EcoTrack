using EcoTrack.Application.Common;
using EcoTrack.Domain.Licencas;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Licencas;

public sealed record ListarLicencasPorEmpreendimentoQuery(Guid EmpreendimentoId) : IRequest<Result<List<LicencaDto>>>;

public sealed class ListarLicencasPorEmpreendimentoQueryHandler
    : IRequestHandler<ListarLicencasPorEmpreendimentoQuery, Result<List<LicencaDto>>>
{
    private readonly ILicencaRepository _licencaRepository;

    public ListarLicencasPorEmpreendimentoQueryHandler(ILicencaRepository licencaRepository)
    {
        _licencaRepository = licencaRepository;
    }

    public async Task<Result<List<LicencaDto>>> Handle(
        ListarLicencasPorEmpreendimentoQuery request,
        CancellationToken cancellationToken)
    {
        var licencas = await _licencaRepository.SelecionarPorEmpreendimentoIdAsync(
            request.EmpreendimentoId,
            cancellationToken);

        var dtos = licencas.Select(DtoMapper.ToDto).ToList();

        return Result.Ok(dtos);
    }
}