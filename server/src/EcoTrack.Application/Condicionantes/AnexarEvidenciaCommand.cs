using EcoTrack.Application.Common;
using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Condicionantes;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Condicionantes;

public sealed record AnexarEvidenciaRequest(
    Guid CondicionanteId,
    Stream Conteudo,
    string NomeOriginalArquivo,
    string ContentType,
    long TamanhoBytes,
    string? NumeroArt = null,
    string? Observacao = null
);

public sealed record AnexarEvidenciaCommand(AnexarEvidenciaRequest Request) : IRequest<Result<Guid>>;

public sealed class AnexarEvidenciaCommandHandler : IRequestHandler<AnexarEvidenciaCommand, Result<Guid>>
{
    private readonly ICondicionanteRepository _condicionanteRepository;
    private readonly IEvidenciaStorageService _storageService;
    private readonly IUserContext _userContext;

    public AnexarEvidenciaCommandHandler(
        ICondicionanteRepository condicionanteRepository,
        IEvidenciaStorageService storageService,
        IUserContext userContext)
    {
        _condicionanteRepository = condicionanteRepository;
        _storageService = storageService;
        _userContext = userContext;
    }

    public async Task<Result<Guid>> Handle(AnexarEvidenciaCommand requestModel, CancellationToken cancellationToken)
    {
        var req = requestModel.Request;

        if (_userContext.TenantId is null)
        {
            return Result.Fail<Guid>(Erros.Criar("Tenant não identificado na sessão.", "TenantId"));
        }

        // 1. Validação do arquivo (RF04.2) usando as regras da entidade
        var errosArquivo = ValidarArquivo(req);
        if (errosArquivo.Count > 0)
        {
            return Result.Fail<Guid>(Erros.Validacao(errosArquivo));
        }

        // 2. Busca a condicionante com evidências (já filtrada por TenantId no DbContext)
        var condicionante = await _condicionanteRepository.SelecionarPorIdComEvidenciasAsync(
            req.CondicionanteId,
            cancellationToken);

        if (condicionante is null)
        {
            return Result.Fail<Guid>(Erros.Criar("Condicionante não encontrada.", "CondicionanteId"));
        }

        var tenantId = _userContext.TenantId.Value;
        var userId = _userContext.UserId ?? Guid.Empty;

        // 3. Salva o conteúdo no storage
        var identificadorArmazenamento = await _storageService.SalvarAsync(
            req.Conteudo,
            req.NomeOriginalArquivo.Trim(),
            req.ContentType.Trim(),
            tenantId,
            cancellationToken);

        // 4. Cria e anexa a evidência no agregado (RF03.4: Pendente -> EmAndamento)
        var evidencia = new EvidenciaCondicionante
        {
            CondicionanteId = condicionante.Id,
            TenantId = tenantId,
            EnviadoPorUserId = userId,
            NomeOriginalArquivo = req.NomeOriginalArquivo.Trim(),
            IdentificadorArmazenamento = identificadorArmazenamento,
            ContentType = req.ContentType.Trim(),
            TamanhoBytes = req.TamanhoBytes,
            NumeroArt = req.NumeroArt,
            Observacao = req.Observacao,
            DataUploadUtc = DateTime.UtcNow
        };

        var errosAnexo = condicionante.AnexarEvidencia(evidencia);
        if (errosAnexo.Count > 0)
        {
            await _storageService.RemoverAsync(identificadorArmazenamento, tenantId, cancellationToken);
            return Result.Fail<Guid>(Erros.Validacao(errosAnexo));
        }

        // 5. Persiste no banco; se falhar, desfaz o upload no storage para não deixar arquivo órfão
        var atualizado = await _condicionanteRepository.EditarAsync(condicionante, cancellationToken);
        if (!atualizado)
        {
            await _storageService.RemoverAsync(identificadorArmazenamento, tenantId, cancellationToken);
            return Result.Fail<Guid>(Erros.Criar("Não foi possível anexar a evidência à condicionante."));
        }

        return Result.Ok(evidencia.Id);
    }

    private static List<string> ValidarArquivo(AnexarEvidenciaRequest req)
    {
        var erros = new List<string>();

        if (req.CondicionanteId == Guid.Empty)
            erros.Add("A condicionante é obrigatória.");

        if (req.Conteudo is null || req.TamanhoBytes <= 0)
            erros.Add("O arquivo de evidência é obrigatório e não pode estar vazio.");

        if (req.TamanhoBytes > EvidenciaCondicionante.TamanhoMaximoBytesPadrao)
        {
            var limiteMb = EvidenciaCondicionante.TamanhoMaximoBytesPadrao / (1024 * 1024);
            erros.Add($"O arquivo excede o tamanho máximo permitido de {limiteMb} MB.");
        }

        if (string.IsNullOrWhiteSpace(req.NomeOriginalArquivo))
        {
            erros.Add("O nome do arquivo é obrigatório.");
        }
        else
        {
            var extensao = Path.GetExtension(req.NomeOriginalArquivo).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extensao) || !EvidenciaCondicionante.ExtensoesPermitidas.Contains(extensao))
            {
                erros.Add(
                    $"Formato de arquivo não permitido ({extensao}). Permitidos: {string.Join(", ", EvidenciaCondicionante.ExtensoesPermitidas)}."
                );
            }
        }

        if (string.IsNullOrWhiteSpace(req.ContentType))
            erros.Add("O Content-Type do arquivo é obrigatório.");

        return erros;
    }
}