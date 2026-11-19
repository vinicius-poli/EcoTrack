using EcoTrack.Application.Common;
using EcoTrack.Domain.Licencas;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Licencas;

public sealed record SelecionarLicencaPorIdQuery(Guid Id) : IRequest<Result<LicencaDto>>;

public sealed class SelecionarLicencaPorIdQueryHandler : IRequestHandler<SelecionarLicencaPorIdQuery, Result<LicencaDto>>
{
    private readonly ILicencaRepository _licencaRepository;

    public SelecionarLicencaPorIdQueryHandler(ILicencaRepository licencaRepository)
    {
        _licencaRepository = licencaRepository;
    }

    public async Task<Result<LicencaDto>> Handle(SelecionarLicencaPorIdQuery request, CancellationToken cancellationToken)
    {
        var licenca = await _licencaRepository.SelecionarPorIdAsync(request.Id, cancellationToken);
        if (licenca is null)
        {
            return Result.Fail<LicencaDto>(Erros.Criar("Licença não encontrada.", "Id"));
        }

        return Result.Ok(DtoMapper.ToDto(licenca));
    }
}