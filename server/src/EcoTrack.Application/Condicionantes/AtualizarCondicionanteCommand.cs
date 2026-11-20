using EcoTrack.Application.Common;
using EcoTrack.Domain.Condicionantes;
using EcoTrack.Domain.Licencas;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Condicionantes;

public sealed record AtualizarCondicionanteRequest(
    Guid LicencaId,
    string CodigoItem,
    string Titulo,
    string Descricao,
    PeriodicidadeCondicionante Periodicidade,
    DateTime DataVencimento,
    string? ResponsavelTecnico,
    StatusCondicionante Status
);

public sealed record AtualizarCondicionanteCommand(Guid Id, AtualizarCondicionanteRequest Request) : IRequest<Result>;

public sealed class AtualizarCondicionanteCommandHandler : IRequestHandler<AtualizarCondicionanteCommand, Result>
{
    private readonly ICondicionanteRepository _condicionanteRepository;
    private readonly ILicencaRepository _licencaRepository;

    public AtualizarCondicionanteCommandHandler(
        ICondicionanteRepository condicionanteRepository,
        ILicencaRepository licencaRepository)
    {
        _condicionanteRepository = condicionanteRepository;
        _licencaRepository = licencaRepository;
    }

    public async Task<Result> Handle(AtualizarCondicionanteCommand requestModel, CancellationToken cancellationToken)
    {
        var id = requestModel.Id;
        var req = requestModel.Request;

        // 1. Carrega a condicionante com evidências (já filtrada por TenantId no DbContext)
        var condicionanteExistente = await _condicionanteRepository.SelecionarPorIdComEvidenciasAsync(
            id,
            cancellationToken);

        if (condicionanteExistente is null)
        {
            return Result.Fail(Erros.Criar("Condicionante não encontrada.", "Id"));
        }

        // 2. Valida alterações de status com base nas evidências anexadas (RF03.3)
        var errosStatus = condicionanteExistente.AlterarStatus(req.Status);
        if (errosStatus.Count > 0)
        {
            return Result.Fail(Erros.Validacao(errosStatus));
        }

        // 3. Aplica atualização de campos na entidade
        var dadosAtualizados = new Condicionante
        {
            LicencaId = req.LicencaId,
            CodigoItem = req.CodigoItem,
            Titulo = req.Titulo,
            Descricao = req.Descricao,
            Periodicidade = req.Periodicidade,
            DataVencimento = req.DataVencimento,
            ResponsavelTecnico = req.ResponsavelTecnico,
            Status = req.Status
        };

        condicionanteExistente.Atualizar(dadosAtualizados);

        // 4. Valida os campos e regras da entidade
        var errosValidacao = condicionanteExistente.Validar();
        if (errosValidacao.Count > 0)
        {
            return Result.Fail(Erros.Validacao(errosValidacao));
        }

        // 5. RF03.1: Garante que a licença informada existe no tenant atual
        var licencaExiste = await _licencaRepository.SelecionarPorIdAsync(
            condicionanteExistente.LicencaId,
            cancellationToken);

        if (licencaExiste is null)
        {
            return Result.Fail(Erros.Criar("A licença informada não foi encontrada.", "LicencaId"));
        }

        // 6. Persiste a alteração
        var atualizado = await _condicionanteRepository.EditarAsync(condicionanteExistente, cancellationToken);
        if (!atualizado)
        {
            return Result.Fail(Erros.Criar("Não foi possível atualizar a condicionante."));
        }

        return Result.Ok();
    }
}