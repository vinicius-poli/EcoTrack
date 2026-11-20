namespace EcoTrack.Domain.Condicionantes;

public interface IEvidenciaStorageService
{
    Task<string> SalvarAsync(
        Stream conteudo,
        string nomeOriginalArquivo,
        string contentType,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Stream?> ObterStreamAsync(
        string identificadorArmazenamento,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task RemoverAsync(
        string identificadorArmazenamento,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}