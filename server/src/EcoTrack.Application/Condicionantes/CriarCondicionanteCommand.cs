using EcoTrack.Application.Common;
using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Condicionantes;
using EcoTrack.Domain.Licencas;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Condicionantes;

public sealed record CriarCondicionanteRequest(
    Guid LicencaId,
    string CodigoItem,
    string Titulo,
    string Descricao,
    PeriodicidadeCondicionante Periodicidade,
    DateTime DataVencimento,
    string? ResponsavelTecnico = null
);

public sealed record CriarCondicionanteCommand(CriarCondicionanteRequest Request) : IRequest<Result<Guid>>;

public sealed class CriarCondicionanteCommandHandler : IRequestHandler<CriarCondicionanteCommand, Result<Guid>>
{
    private readonly ICondicionanteRepository _condicionanteRepository;
    private readonly ILicencaRepository _licencaRepository;
    private readonly IUserContext _userContext;

    public CriarCondicionanteCommandHandler(
        ICondicionanteRepository condicionanteRepository,
        ILicencaRepository licencaRepository,
        IUserContext userContext)
    {
        _condicionanteRepository = condicionanteRepository;
        _licencaRepository = licencaRepository;
        _userContext = userContext;
    }

    public async Task<Result<Guid>> Handle(CriarCondicionanteCommand requestModel, CancellationToken cancellationToken)
    {
        var req = requestModel.Request;

        if (_userContext.TenantId is null)
        {
            return Result.Fail<Guid>(Erros.Criar("Tenant não identificado na sessão.", "TenantId"));
        }

        var condicionante = new Condicionante
        {
            LicencaId = req.LicencaId,
            CodigoItem = req.CodigoItem,
            Titulo = req.Titulo,
            Descricao = req.Descricao,
            Periodicidade = req.Periodicidade,
            DataVencimento = req.DataVencimento,
            ResponsavelTecnico = req.ResponsavelTecnico,
            Status = StatusCondicionante.Pendente,
            TenantId = _userContext.TenantId.Value,
            UserId = _userContext.UserId ?? Guid.Empty
        };

        // 1. Validação da entidade (normaliza campos primeiro)
        var errosValidacao = condicionante.Validar();
        if (errosValidacao.Count > 0)
        {
            return Result.Fail<Guid>(Erros.Validacao(errosValidacao));
        }

        // 2. RF03.1: Licença precisa existir (já filtrada pelo TenantId no DbContext)
        var licencaExiste = await _licencaRepository.SelecionarPorIdAsync(
            condicionante.LicencaId,
            cancellationToken);

        if (licencaExiste is null)
        {
            return Result.Fail<Guid>(Erros.Criar("A licença informada não foi encontrada.", "LicencaId"));
        }

        // 3. Persistência
        await _condicionanteRepository.CadastrarAsync(condicionante, cancellationToken);

        return Result.Ok(condicionante.Id);
    }
}