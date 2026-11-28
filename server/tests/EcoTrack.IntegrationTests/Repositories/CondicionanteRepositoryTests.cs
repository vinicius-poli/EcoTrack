using EcoTrack.Domain.Condicionantes;
using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Domain.Licencas;
using EcoTrack.IntegrationTests.Infrastructure;
using Testcontainers.MsSql;

namespace EcoTrack.IntegrationTests.Repositories;

[TestClass]
public sealed class CondicionanteRepositoryTests
{
    private static MsSqlContainer _sqlContainer = null!;
    private static IntegrationTestFixture _fixture = null!;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext _)
    {
        _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await _sqlContainer.StartAsync();

        _fixture = new IntegrationTestFixture(_sqlContainer.GetConnectionString());
        await _fixture.InitializeAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
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
        var sufixoUnico = Guid.NewGuid().ToString("N")[..6];
        var cnpjUnico = Random.Shared.NextInt64(10_000_000_000_000, 99_999_999_999_999).ToString("D14");

        var empreendimento = new Empreendimento
        {
            Id = empreendimentoId,
            TenantId = tenantId,
            UserId = userId,
            RazaoSocial = $"Empreendimento {sufixoUnico} Ltda",
            NomeFantasia = $"Unidade {sufixoUnico}",
            Cnpj = cnpjUnico,
            Municipio = "Rio do Sul",
            Uf = "SC",
            Porte = default,
            StatusLicenciamento = StatusLicenciamento.EmAnalise
        };

        context.Set<Empreendimento>().Add(empreendimento);
        await context.SaveChangesAsync();

        var licenca = new Licenca
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            EmpreendimentoId = empreendimentoId,
            NumeroProcesso = $"PROC-{Guid.NewGuid().ToString("N")[..8]}",
            NumeroLicenca = $"LIC-{Guid.NewGuid().ToString("N")[..8]}",
            OrgaoEmissor = "IMA",
            Tipo = default,
            DataEmissao = DateTime.UtcNow.AddDays(-30),
            DataValidade = DateTime.UtcNow.AddYears(2),
            Status = StatusLicenca.Vigente
        };

        context.Set<Licenca>().Add(licenca);
        await context.SaveChangesAsync();

        return licenca.Id;
    }

    private static Condicionante CriarCondicionanteValida(
        Guid tenantId,
        Guid userId,
        Guid licencaId,
        string codigoItem = "COND-01",
        string titulo = "Monitoramento de efluentes")
    {
        return new Condicionante
        {
            UserId = userId,
            TenantId = tenantId,
            LicencaId = licencaId,
            CodigoItem = codigoItem,
            Titulo = titulo,
            Descricao = "Coleta e análise trimestral conforme licença ambiental.",
            Periodicidade = PeriodicidadeCondicionante.Trimestral,
            DataVencimento = DateTime.UtcNow.AddDays(45),
            ResponsavelTecnico = "Eng. Ambiental Teste",
            Status = StatusCondicionante.Pendente
        };
    }

    private static EvidenciaCondicionante CriarEvidenciaValida(
        Guid tenantId,
        Guid userId,
        string nomeArquivo,
        string numeroArt = "ART-2026-001")
    {
        return new EvidenciaCondicionante
        {
            TenantId = tenantId,
            EnviadoPorUserId = userId,
            NomeOriginalArquivo = nomeArquivo,
            IdentificadorArmazenamento = $"{tenantId:D}/{Guid.NewGuid():N}{Path.GetExtension(nomeArquivo)}",
            ContentType = "application/pdf",
            TamanhoBytes = 2048,
            NumeroArt = numeroArt,
            Observacao = "Evidência técnica anexada no teste de integração."
        };
    }

    [TestMethod]
    public async Task SelecionarPorIdAsync_deve_respeitar_isolamento_por_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userIdA = Guid.NewGuid();
        var userIdB = Guid.NewGuid();
        var licencaIdA = await CriarLicencaAsync(tenantA, userIdA);

        var repoTenantA = _fixture.CreateCondicionanteRepository(new TestUserContext
        {
            TenantId = tenantA,
            UserId = userIdA,
            Email = "tenant-a@ecotrack.test",
            Perfil = "Gestor"
        });

        var repoTenantB = _fixture.CreateCondicionanteRepository(new TestUserContext
        {
            TenantId = tenantB,
            UserId = userIdB,
            Email = "tenant-b@ecotrack.test",
            Perfil = "Gestor"
        });

        var condicionante = CriarCondicionanteValida(tenantA, userIdA, licencaIdA);

        await repoTenantA.CadastrarAsync(condicionante, CancellationToken.None);

        var encontradaNoTenantA = await repoTenantA.SelecionarPorIdAsync(condicionante.Id, CancellationToken.None);
        var encontradaNoTenantB = await repoTenantB.SelecionarPorIdAsync(condicionante.Id, CancellationToken.None);
        var porLicencaNoTenantB = await repoTenantB.SelecionarPorLicencaIdAsync(licencaIdA, CancellationToken.None);

        Assert.IsNotNull(encontradaNoTenantA);
        Assert.AreEqual(condicionante.Id, encontradaNoTenantA.Id);
        Assert.IsNull(encontradaNoTenantB);
        Assert.IsEmpty(porLicencaNoTenantB);
    }

    [TestMethod]
    public async Task CadastrarAsync_e_EditarAsync_deve_atualizar_campos_e_retornar_true()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var licencaId = await CriarLicencaAsync(tenantId, userId);

        var repo = _fixture.CreateCondicionanteRepository(new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "gestor@ecotrack.test",
            Perfil = "Admin"
        });

        var condicionante = CriarCondicionanteValida(tenantId, userId, licencaId, "COND-02", "Título Inicial");
        await repo.CadastrarAsync(condicionante, CancellationToken.None);

        var atualizada = CriarCondicionanteValida(tenantId, userId, licencaId, "COND-02", "Título Atualizado");
        atualizada.Id = condicionante.Id;
        atualizada.Status = StatusCondicionante.EmAndamento;

        var editou = await repo.EditarAsync(atualizada, CancellationToken.None);
        var recuperada = await repo.SelecionarPorIdAsync(condicionante.Id, CancellationToken.None);

        Assert.IsTrue(editou);
        Assert.IsNotNull(recuperada);
        Assert.AreEqual("Título Atualizado", recuperada.Titulo);
        Assert.AreEqual(StatusCondicionante.EmAndamento, recuperada.Status);
    }

    [TestMethod]
    public async Task ExcluirAsync_deve_remover_registro_existente_e_retornar_false_se_inexistente()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var licencaId = await CriarLicencaAsync(tenantId, userId);

        var repo = _fixture.CreateCondicionanteRepository(new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "admin@ecotrack.test",
            Perfil = "Admin"
        });

        var condicionante = CriarCondicionanteValida(tenantId, userId, licencaId, "COND-03", "Para Exclusão");
        await repo.CadastrarAsync(condicionante, CancellationToken.None);

        var excluiu = await repo.ExcluirAsync(condicionante.Id, CancellationToken.None);
        var aposExclusao = await repo.SelecionarPorIdAsync(condicionante.Id, CancellationToken.None);
        var segundoExcluir = await repo.ExcluirAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsTrue(excluiu);
        Assert.IsNull(aposExclusao);
        Assert.IsFalse(segundoExcluir);
    }

    [TestMethod]
    public async Task EditarAsync_deve_sincronizar_adicao_e_remocao_de_evidencias()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var licencaId = await CriarLicencaAsync(tenantId, userId);

        var repo = _fixture.CreateCondicionanteRepository(new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "gestor@ecotrack.test",
            Perfil = "Admin"
        });

        var condicionante = CriarCondicionanteValida(tenantId, userId, licencaId, "COND-04", "Monitoramento Atmosférico");

        var evidenciaMantida = CriarEvidenciaValida(tenantId, userId, "laudo-chamine-q1.pdf", "ART-101");
        var evidenciaRemovida = CriarEvidenciaValida(tenantId, userId, "laudo-rascunho.pdf", "ART-102");

        var errosAnexo1 = condicionante.AnexarEvidencia(evidenciaMantida);
        var errosAnexo2 = condicionante.AnexarEvidencia(evidenciaRemovida);

        Assert.IsEmpty(errosAnexo1);
        Assert.IsEmpty(errosAnexo2);
        Assert.AreEqual(StatusCondicionante.EmAndamento, condicionante.Status);

        await repo.CadastrarAsync(condicionante, CancellationToken.None);

        var paraEditar = await repo.SelecionarPorIdComEvidenciasAsync(condicionante.Id, CancellationToken.None);
        Assert.IsNotNull(paraEditar);
        Assert.HasCount(2, paraEditar.Evidencias);

        var errosRemocao = paraEditar.RemoverEvidencia(evidenciaRemovida.Id);
        var evidenciaNova = CriarEvidenciaValida(tenantId, userId, "laudo-chamine-q2.pdf", "ART-103");
        var errosAnexo3 = paraEditar.AnexarEvidencia(evidenciaNova);

        Assert.IsEmpty(errosRemocao);
        Assert.IsEmpty(errosAnexo3);

        var editou = await repo.EditarAsync(paraEditar, CancellationToken.None);
        Assert.IsTrue(editou);

        var aposEdicao = await repo.SelecionarPorIdComEvidenciasAsync(condicionante.Id, CancellationToken.None);
        Assert.IsNotNull(aposEdicao);
        Assert.HasCount(2, aposEdicao.Evidencias);

        var idsPersistidos = aposEdicao.Evidencias.Select(e => e.Id).ToHashSet();
        Assert.Contains(evidenciaMantida.Id, idsPersistidos);
        Assert.Contains(evidenciaNova.Id, idsPersistidos);
        Assert.DoesNotContain(evidenciaRemovida.Id, idsPersistidos);
    }
}