using System.Text;
using EcoTrack.Domain.Condicionantes;
using EcoTrack.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace EcoTrack.IntegrationTests.Storage;

[TestClass]
public sealed class LocalEvidenciaStorageServiceTests
{
    private static string _pastaTemporaria = null!;
    private static LocalEvidenciaStorageService _storageService = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        _pastaTemporaria = Path.Combine(
            Path.GetTempPath(),
            "EcoTrack.IntegrationTests.Storage",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_pastaTemporaria);

        var options = Options.Create(new EvidenciaStorageOptions
        {
            BasePath = _pastaTemporaria
        });

        _storageService = new LocalEvidenciaStorageService(options);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        if (Directory.Exists(_pastaTemporaria))
        {
            Directory.Delete(_pastaTemporaria, recursive: true);
        }
    }

    [TestMethod]
    public async Task SalvarAsync_ObterStreamAsync_e_RemoverAsync_devem_executar_ciclo_completo_por_tenant()
    {
        var tenantId = Guid.NewGuid();
        const string conteudoOriginal = "%PDF-1.4 Evidência de monitoramento ambiental";
        await using var streamEntrada = new MemoryStream(Encoding.UTF8.GetBytes(conteudoOriginal));

        var identificador = await _storageService.SalvarAsync(
            streamEntrada,
            "laudo-efluentes.pdf",
            "application/pdf",
            tenantId,
            CancellationToken.None);

        Assert.IsFalse(string.IsNullOrWhiteSpace(identificador));
        Assert.IsTrue(identificador.StartsWith($"{tenantId:D}/", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(identificador.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

        string conteudoLido;
        await using (var streamLido = await _storageService.ObterStreamAsync(
            identificador,
            tenantId,
            CancellationToken.None))
        {
            Assert.IsNotNull(streamLido);

            using var reader = new StreamReader(streamLido, Encoding.UTF8);
            conteudoLido = await reader.ReadToEndAsync();
        }

        Assert.AreEqual(conteudoOriginal, conteudoLido);

        await _storageService.RemoverAsync(identificador, tenantId, CancellationToken.None);

        var streamAposRemocao = await _storageService.ObterStreamAsync(
            identificador,
            tenantId,
            CancellationToken.None);

        Assert.IsNull(streamAposRemocao);
    }

    [TestMethod]
    public async Task ObterStreamAsync_deve_retornar_null_quando_acessado_por_outro_tenant()
    {
        var tenantDono = Guid.NewGuid();
        var tenantInvasor = Guid.NewGuid();
        await using var streamEntrada = new MemoryStream("conteudo-restrito"u8.ToArray());

        var identificador = await _storageService.SalvarAsync(
            streamEntrada,
            "relatorio.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            tenantDono,
            CancellationToken.None);

        await using (var streamOutroTenant = await _storageService.ObterStreamAsync(
            identificador,
            tenantInvasor,
            CancellationToken.None))
        {
            Assert.IsNull(streamOutroTenant);
        }

        await _storageService.RemoverAsync(identificador, tenantDono, CancellationToken.None);
    }

    [TestMethod]
    public async Task SalvarAsync_deve_descartar_extensao_nao_permitida_e_ObterStreamAsync_deve_bloquear_path_traversal()
    {
        var tenantId = Guid.NewGuid();
        await using var streamEntrada = new MemoryStream("script-ou-texto"u8.ToArray());

        var identificador = await _storageService.SalvarAsync(
            streamEntrada,
            "payload.exe",
            "application/octet-stream",
            tenantId,
            CancellationToken.None);

        Assert.AreEqual(string.Empty, Path.GetExtension(identificador));

        await using (var streamTraversal = await _storageService.ObterStreamAsync(
            "../outro-tenant/arquivo.pdf",
            tenantId,
            CancellationToken.None))
        {
            Assert.IsNull(streamTraversal);
        }

        await _storageService.RemoverAsync(identificador, tenantId, CancellationToken.None);
    }
}