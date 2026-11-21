using EcoTrack.Application.Common;
using EcoTrack.Domain.Condicionantes;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Condicionantes;

public sealed record ExcluirCondicionanteCommand(Guid Id) : IRequest<Result>;

public sealed class ExcluirCondicionanteCommandHandler : IRequestHandler<ExcluirCondicionanteCommand, Result>
{
    private readonly ICondicionanteRepository _condicionanteRepository;
    private readonly IEvidenciaStorageService _evidenciaStorageService;

    public ExcluirCondicionanteCommandHandler(
        ICondicionanteRepository condicionanteRepository,
        IEvidenciaStorageService evidenciaStorageService)
    {
        _condicionanteRepository = condicionanteRepository;
        _evidenciaStorageService = evidenciaStorageService;
    }

    public async Task<Result> Handle(ExcluirCondicionanteCommand request, CancellationToken cancellationToken)
    {
        // 1. Carrega a condicionante com evidências (já filtrada por TenantId no DbContext)
        var condicionanteExistente = await _condicionanteRepository.SelecionarPorIdComEvidenciasAsync(
            request.Id,
            cancellationToken);

        if (condicionanteExistente is null)
        {
            return Result.Fail(Erros.Criar("Condicionante não encontrada.", "Id"));
        }

        var evidencias = condicionanteExistente.Evidencias.ToList();
        var quantidadeEvidencias = evidencias.Count;

        // 2. Remove os arquivos físicos no storage antes de excluir os registros no banco
        foreach (var evidencia in evidencias)
        {
            await _evidenciaStorageService.RemoverAsync(
                evidencia.IdentificadorArmazenamento,
                evidencia.TenantId,
                cancellationToken);
        }

        // 3. Executa a exclusão no repositório (Cascade remove os registros de evidência no banco)
        var excluida = await _condicionanteRepository.ExcluirAsync(request.Id, cancellationToken);
        if (!excluida)
        {
            return Result.Fail(Erros.Criar("Não foi possível excluir a condicionante."));
        }

        // 4. Emite alerta caso existissem evidências associadas que foram removidas junto
        if (quantidadeEvidencias > 0)
        {
            var sufixo = quantidadeEvidencias == 1 ? "evidência foi removida" : "evidências foram removidas";
            return Result.Ok().WithSuccess(
                $"Atenção: {quantidadeEvidencias} {sufixo} permanentemente junto com a condicionante."
            );
        }

        return Result.Ok();
    }
}