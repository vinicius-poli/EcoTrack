using EcoTrack.Application.Common;
using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Condicionantes;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Condicionantes;

public sealed record BaixarEvidenciaResponse(
    Stream Conteudo,
    string NomeOriginalArquivo,
    string ContentType,
    long TamanhoBytes
);

public sealed record BaixarEvidenciaQuery(Guid CondicionanteId, Guid EvidenciaId)
    : IRequest<Result<BaixarEvidenciaResponse>>;

public sealed class BaixarEvidenciaQueryHandler
    : IRequestHandler<BaixarEvidenciaQuery, Result<BaixarEvidenciaResponse>>
{
    private readonly ICondicionanteRepository _condicionanteRepository;
    private readonly IEvidenciaStorageService _storageService;
    private readonly IUserContext _userContext;

    public BaixarEvidenciaQueryHandler(
        ICondicionanteRepository condicionanteRepository,
        IEvidenciaStorageService storageService,
        IUserContext userContext)
    {
        _condicionanteRepository = condicionanteRepository;
        _storageService = storageService;
        _userContext = userContext;
    }

    public async Task<Result<BaixarEvidenciaResponse>> Handle(
        BaixarEvidenciaQuery request,
        CancellationToken cancellationToken)
    {
        if (_userContext.TenantId is null)
        {
            return Result.Fail<BaixarEvidenciaResponse>(
                Erros.Criar("Tenant não identificado na sessão.", "TenantId")
            );
        }

        if (request.CondicionanteId == Guid.Empty)
        {
            return Result.Fail<BaixarEvidenciaResponse>(
                Erros.Criar("A condicionante é obrigatória.", "CondicionanteId")
            );
        }

        if (request.EvidenciaId == Guid.Empty)
        {
            return Result.Fail<BaixarEvidenciaResponse>(
                Erros.Criar("A evidência é obrigatória.", "EvidenciaId")
            );
        }

        // 1. Busca a condicionante com evidências (já filtrada por TenantId no DbContext)
        var condicionante = await _condicionanteRepository.SelecionarPorIdComEvidenciasAsync(
            request.CondicionanteId,
            cancellationToken);

        if (condicionante is null)
        {
            return Result.Fail<BaixarEvidenciaResponse>(
                Erros.Criar("Condicionante não encontrada.", "CondicionanteId")
            );
        }

        // 2. Localiza a evidência dentro da condicionante
        var evidencia = condicionante.Evidencias.FirstOrDefault(e => e.Id == request.EvidenciaId);
        if (evidencia is null)
        {
            return Result.Fail<BaixarEvidenciaResponse>(
                Erros.Criar("Evidência não encontrada na condicionante.", "EvidenciaId")
            );
        }

        // 3. Obtém o stream do arquivo no storage
        var stream = await _storageService.ObterStreamAsync(
            evidencia.IdentificadorArmazenamento,
            _userContext.TenantId.Value,
            cancellationToken);

        if (stream is null)
        {
            return Result.Fail<BaixarEvidenciaResponse>(
                Erros.Criar("O arquivo da evidência não foi encontrado no armazenamento.", "EvidenciaId")
            );
        }

        var response = new BaixarEvidenciaResponse(
            Conteudo: stream,
            NomeOriginalArquivo: evidencia.NomeOriginalArquivo,
            ContentType: evidencia.ContentType,
            TamanhoBytes: evidencia.TamanhoBytes
        );

        return Result.Ok(response);
    }
}