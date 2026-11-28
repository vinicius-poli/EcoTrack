using System.Text;
using EcoTrack.Domain.Condicionantes;
using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Domain.Licencas;
using EcoTrack.Infrastructure.Storage;
using EcoTrack.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Options;
using Testcontainers.MsSql;

namespace EcoTrack.IntegrationTests.Integration;

[TestClass]
public sealed class EvidenciaFluxoIntegradoTests
{
    private static MsSqlContainer _sqlContainer = null!;
    private static IntegrationTestFixture _fixture = null!;
    private static string _pastaStorageTemporaria = null!;
    private static LocalEvidenciaStorageService _storageService = null!;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext _)
    {
        _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await _sqlContainer.StartAsync();

        _fixture = new IntegrationTestFixture(_sqlContainer.GetConnectionString());
        await _fixture.InitializeAsync();

        _pastaStorageTemporaria = Path.Combine(
            Path.GetTempPath(),
            "EcoTrack.IntegrationTests.FluxoEvidencia",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_pastaStorageTemporaria);

        _storageService = new LocalEvidenciaStorageService(
            Options.Create(new EvidenciaStorageOptions { BasePath = _pastaStorageTemporaria }));
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        if (Directory.Exists(_pastaStorageTemporaria))
        {
            try
            {
                Directory.Delete(_pastaStorageTemporaria, recursive: true);
            }
            catch (IOException)
            {
                // Evita falha transitória de handle no Windows durante limpeza
            }
        }

        if (_fixture is not null)
            await _fixture.DisposeAsync();

        if (_sqlContainer is not null)
            await _sqlContainer.DisposeAsync();
    }

    private static async Task<Guid> CriarLicencaAsync(Guid tenantId, Guid userId)
    {
        await using var context = _fixture.CreateDbContext(new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "seed@ecotrack.test",
            Perfil = "Admin"
        });

        var empreendimentoId = Guid.NewGuid();
        var sufixo = Guid.NewGuid().ToString("N")[..6];
        var cnpjUnico = Random.Shared.NextInt64(10_000_000_000_000, 99_999_999_999_999).ToString("D14");

        context.Set<Empreendimento>().Add(new Empreendimento
        {
            Id = empreendimentoId,
            TenantId = tenantId,
            UserId = userId,
            RazaoSocial = $"Empreendimento {sufixo} Ltda",
            NomeFantasia = $"Unidade {sufixo}",
            Cnpj = cnpjUnico,
            Municipio = "Rio do Sul",
            Uf = "SC",
            Porte = default,
            StatusLicenciamento = StatusLicenciamento.EmAnalise
        });

        var licencaId = Guid.NewGuid();
        context.Set<Licenca>().Add(new Licenca
        {
            Id = licencaId,
            TenantId = tenantId,
            UserId = userId,
            EmpreendimentoId = empreendimentoId,
            NumeroProcesso = $"PROC-{Guid.NewGuid().ToString("N")[..8]}",
            NumeroLicenca = $"LIC-{Guid.NewGuid().ToString("N")[..8]}",
            OrgaoEmissor = "IMA",
            Tipo = default,
            DataEmissao = DateTime.UtcNow.AddDays(-20),
            DataValidade = DateTime.UtcNow.AddYears(2),
            Status = StatusLicenca.Vigente
        });

        await context.SaveChangesAsync();
        return licencaId;
    }

    [TestMethod]
    public async Task Fluxo_completo_deve_salvar_arquivo_persistir_evidencia_e_remover_ambos()
    {
        var tenantId = Guid.NewGuid();
        var outroTenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var licencaId = await CriarLicencaAsync(tenantId, userId);

        var repo = _fixture.CreateCondicionanteRepository(new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "gestor@ecotrack.test",
            Perfil = "Admin"
        });

        const string conteudoLaudo = "%PDF-1.4 Relatório de monitoramento ambiental";
        const string nomeOriginal = "relatorio-monitoramento.pdf";
        const string contentType = "application/pdf";

        await using var streamEntrada = new MemoryStream(Encoding.UTF8.GetBytes(conteudoLaudo));

        var identificadorArmazenamento = await _storageService.SalvarAsync(
            streamEntrada,
            nomeOriginal,
            contentType,
            tenantId,
            CancellationToken.None);

        var evidencia = new EvidenciaCondicionante
        {
            TenantId = tenantId,
            EnviadoPorUserId = userId,
            NomeOriginalArquivo = nomeOriginal,
            IdentificadorArmazenamento = identificadorArmazenamento,
            ContentType = contentType,
            TamanhoBytes = streamEntrada.Length,
            NumeroArt = "ART-2026-777",
            Observacao = "Arquivo salvo no storage e vinculado à condicionante."
        };

        var condicionante = new Condicionante
        {
            UserId = userId,
            TenantId = tenantId,
            LicencaId = licencaId,
            CodigoItem = "COND-INT-01",
            Titulo = "Controle de emissões e efluentes",
            Descricao = "Anexo de laudo técnico com armazenamento físico.",
            Periodicidade = PeriodicidadeCondicionante.Semestral,
            DataVencimento = DateTime.UtcNow.AddDays(60),
            ResponsavelTecnico = "Eng. Ambiental Responsável",
            Status = StatusCondicionante.Pendente
        };

        var errosAnexo = condicionante.AnexarEvidencia(evidencia);
        Assert.IsEmpty(errosAnexo);

        await repo.CadastrarAsync(condicionante, CancellationToken.None);

        // 1. Confere leitura do arquivo no storage (dono ok, outro tenant null)
        string conteudoLido;
        await using (var streamDono = await _storageService.ObterStreamAsync(
            identificadorArmazenamento,
            tenantId,
            CancellationToken.None))
        {
            Assert.IsNotNull(streamDono);
            using var reader = new StreamReader(streamDono, Encoding.UTF8);
            conteudoLido = await reader.ReadToEndAsync();
        }

        await using (var streamOutroTenant = await _storageService.ObterStreamAsync(
            identificadorArmazenamento,
            outroTenantId,
            CancellationToken.None))
        {
            Assert.IsNull(streamOutroTenant);
        }

        Assert.AreEqual(conteudoLaudo, conteudoLido);

        // 2. Confere evidência persistida no banco
        var persistida = await repo.SelecionarPorIdComEvidenciasAsync(condicionante.Id, CancellationToken.None);
        Assert.IsNotNull(persistida);
        Assert.HasCount(1, persistida.Evidencias);
        Assert.AreEqual(identificadorArmazenamento, persistida.Evidencias.First().IdentificadorArmazenamento);

        // 3. Remove evidência do agregado, persiste no banco e remove arquivo físico no disco
        var errosRemocao = persistida.RemoverEvidencia(evidencia.Id);
        Assert.IsEmpty(errosRemocao);

        var editou = await repo.EditarAsync(persistida, CancellationToken.None);
        Assert.IsTrue(editou);

        await _storageService.RemoverAsync(identificadorArmazenamento, tenantId, CancellationToken.None);

        var aposRemocaoBanco = await repo.SelecionarPorIdComEvidenciasAsync(condicionante.Id, CancellationToken.None);
        Assert.IsNotNull(aposRemocaoBanco);
        Assert.IsEmpty(aposRemocaoBanco.Evidencias);

        await using var streamAposRemocaoDisco = await _storageService.ObterStreamAsync(
            identificadorArmazenamento,
            tenantId,
            CancellationToken.None);
        Assert.IsNull(streamAposRemocaoDisco);
    }
}