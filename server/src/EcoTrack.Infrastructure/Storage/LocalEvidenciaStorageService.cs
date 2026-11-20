using EcoTrack.Domain.Condicionantes;
using Microsoft.Extensions.Options;

namespace EcoTrack.Infrastructure.Storage;

public sealed class LocalEvidenciaStorageService : IEvidenciaStorageService
{
    private readonly string _raizAbsoluta;

    public LocalEvidenciaStorageService(IOptions<EvidenciaStorageOptions> options)
    {
        var basePath = string.IsNullOrWhiteSpace(options.Value.BasePath)
            ? "App_Data/evidencias"
            : options.Value.BasePath;

        _raizAbsoluta = Path.GetFullPath(
            Path.IsPathRooted(basePath)
                ? basePath
                : Path.Combine(AppContext.BaseDirectory, basePath)
        );

        Directory.CreateDirectory(_raizAbsoluta);
    }

    public async Task<string> SalvarAsync(
        Stream conteudo,
        string nomeOriginalArquivo,
        string contentType,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conteudo);

        var pastaTenantAbsoluta = ObterPastaTenantAbsoluta(tenantId);
        Directory.CreateDirectory(pastaTenantAbsoluta);

        var extensao = ExtrairExtensaoSegura(nomeOriginalArquivo);
        var nomeArquivoFisico = $"{Guid.NewGuid():N}{extensao}";
        var caminhoCompleto = ResolverCaminhoSeguro(pastaTenantAbsoluta, nomeArquivoFisico);

        await using var destino = new FileStream(
            caminhoCompleto,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        if (conteudo.CanSeek)
        {
            conteudo.Position = 0;
        }

        await conteudo.CopyToAsync(destino, cancellationToken);

        // Identificador relativo portável: "{tenantId}/{nomeArquivoFisico}"
        return $"{tenantId:D}/{nomeArquivoFisico}";
    }

    public Task<Stream?> ObterStreamAsync(
        string identificadorArmazenamento,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identificadorArmazenamento))
        {
            return Task.FromResult<Stream?>(null);
        }

        var caminhoCompleto = TentarResolverIdentificadorDoTenant(identificadorArmazenamento, tenantId);
        if (caminhoCompleto is null || !File.Exists(caminhoCompleto))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            caminhoCompleto,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task RemoverAsync(
        string identificadorArmazenamento,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identificadorArmazenamento))
        {
            return Task.CompletedTask;
        }

        var caminhoCompleto = TentarResolverIdentificadorDoTenant(identificadorArmazenamento, tenantId);
        if (caminhoCompleto is not null && File.Exists(caminhoCompleto))
        {
            File.Delete(caminhoCompleto);
        }

        return Task.CompletedTask;
    }

    private string ObterPastaTenantAbsoluta(Guid tenantId)
    {
        var pastaTenant = Path.Combine(_raizAbsoluta, tenantId.ToString("D"));
        return ResolverCaminhoSeguro(_raizAbsoluta, pastaTenant);
    }

    private string? TentarResolverIdentificadorDoTenant(string identificadorArmazenamento, Guid tenantId)
    {
        try
        {
            var pastaTenantAbsoluta = ObterPastaTenantAbsoluta(tenantId);

            // Normaliza separadores e remove eventual prefixo repetido do tenantId
            var normalizado = identificadorArmazenamento
                .Replace('\\', '/')
                .TrimStart('/');

            var prefixoTenant = $"{tenantId:D}/";
            if (normalizado.StartsWith(prefixoTenant, StringComparison.OrdinalIgnoreCase))
            {
                normalizado = normalizado[prefixoTenant.Length..];
            }

            // Exige um nome de arquivo simples dentro da pasta do tenant (sem subpastas ou '..')
            var nomeArquivo = Path.GetFileName(normalizado);
            if (string.IsNullOrWhiteSpace(nomeArquivo) || nomeArquivo != normalizado)
            {
                return null;
            }

            return ResolverCaminhoSeguro(pastaTenantAbsoluta, nomeArquivo);
        }
        catch
        {
            return null;
        }
    }

    private static string ResolverCaminhoSeguro(string pastaBaseAbsoluta, string caminhoAlvo)
    {
        var baseComSeparador = pastaBaseAbsoluta.EndsWith(Path.DirectorySeparatorChar)
            ? pastaBaseAbsoluta
            : pastaBaseAbsoluta + Path.DirectorySeparatorChar;

        var caminhoResolvido = Path.GetFullPath(
            Path.IsPathRooted(caminhoAlvo)
                ? caminhoAlvo
                : Path.Combine(baseComSeparador, caminhoAlvo)
        );

        if (!caminhoResolvido.StartsWith(baseComSeparador, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Caminho de armazenamento fora do diretório permitido.");
        }

        return caminhoResolvido;
    }

    private static string ExtrairExtensaoSegura(string nomeOriginalArquivo)
    {
        var extensao = Path.GetExtension(nomeOriginalArquivo).ToLowerInvariant();
        return EvidenciaCondicionante.ExtensoesPermitidas.Contains(extensao)
            ? extensao
            : string.Empty;
    }
}