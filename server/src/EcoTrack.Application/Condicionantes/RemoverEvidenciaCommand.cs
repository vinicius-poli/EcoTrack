using EcoTrack.Application.Common;
using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Condicionantes;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Condicionantes;

public sealed record RemoverEvidenciaCommand(Guid CondicionanteId, Guid EvidenciaId) : IRequest<Result>;

public sealed class RemoverEvidenciaCommandHandler : IRequestHandler<RemoverEvidenciaCommand, Result>
{
    private readonly ICondicionanteRepository _condicionanteRepository;
    private readonly IEvidenciaStorageService _storageService;
    private readonly IUserContext _userContext;

    public RemoverEvidenciaCommandHandler(
        ICondicionanteRepository condicionanteRepository,
        IEvidenciaStorageService storageService,
        IUserContext userContext)
    {
        _condicionanteRepository = condicionanteRepository;
        _storageService = storageService;
        _userContext = userContext;
    }

    public async Task<Result> Handle(RemoverEvidenciaCommand request, CancellationToken cancellationToken)
    {
        if (_userContext.TenantId is null)
        {
            return Result.Fail(Erros.Criar("Tenant não identificado na sessão.", "TenantId"));
        }

        // 1. Busca a condicionante com evidências (já filtrada por TenantId no DbContext)
        var condicionante = await _condicionanteRepository.SelecionarPorIdComEvidenciasAsync(
            request.CondicionanteId,
            cancellationToken);

        if (condicionante is null)
        {
            return Result.Fail(Erros.Criar("Condicionante não encontrada.", "CondicionanteId"));
        }

        var evidenciaAlvo = condicionante.Evidencias.FirstOrDefault(e => e.Id == request.EvidenciaId);
        if (evidenciaAlvo is null)
        {
            return Result.Fail(Erros.Criar("Evidência não encontrada na condicionante.", "EvidenciaId"));
        }

        var identificadorArmazenamento = evidenciaAlvo.IdentificadorArmazenamento;
        var statusAnterior = condicionante.Status;

        // 2. Remove no domínio (reverte Concluida -> EmAndamento se ficar sem evidências)
        var errosRemocao = condicionante.RemoverEvidencia(request.EvidenciaId);
        if (errosRemocao.Count > 0)
        {
            return Result.Fail(Erros.Validacao(errosRemocao));
        }

        // 3. Persiste a remoção no banco
        var atualizado = await _condicionanteRepository.EditarAsync(condicionante, cancellationToken);
        if (!atualizado)
        {
            return Result.Fail(Erros.Criar("Não foi possível remover a evidência."));
        }

        // 4. Remove o arquivo do storage após confirmar a exclusão no banco
        await _storageService.RemoverAsync(
            identificadorArmazenamento,
            _userContext.TenantId.Value,
            cancellationToken);

        // Avisa se a condicionante voltou de Concluída para Em Andamento
        if (statusAnterior == StatusCondicionante.Concluida &&
            condicionante.Status == StatusCondicionante.EmAndamento)
        {
            return Result.Ok().WithSuccess(
                "Evidência removida e status da condicionante revertido de Concluída para Em Andamento por não haver mais evidências."
            );
        }

        return Result.Ok();
    }
}