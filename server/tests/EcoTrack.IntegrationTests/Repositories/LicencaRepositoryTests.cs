using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Domain.Licencas;
using EcoTrack.IntegrationTests.Infrastructure;
using Testcontainers.MsSql;

namespace EcoTrack.IntegrationTests.Repositories;

[TestClass]
public sealed class LicencaRepositoryTests
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

    private static async Task<Guid> CriarEmpreendimentoAsync(Guid tenantId, Guid userId)
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

        return empreendimentoId;
    }

    private static Licenca CriarLicencaValida(
        Guid tenantId,
        Guid userId,
        Guid empreendimentoId,
        string? numeroLicenca = null,
        string orgaoEmissor = "IMA")
    {
        return new Licenca
        {
            UserId = userId,
            TenantId = tenantId,
            EmpreendimentoId = empreendimentoId,
            NumeroProcesso = $"PROC-{Guid.NewGuid().ToString("N")[..8]}",
            NumeroLicenca = numeroLicenca ?? $"LIC-{Guid.NewGuid().ToString("N")[..8]}",
            OrgaoEmissor = orgaoEmissor,
            Tipo = default,
            DataEmissao = DateTime.UtcNow.AddDays(-60),
            DataValidade = DateTime.UtcNow.AddYears(2),
            DiasAntecedenciaRenovacao = Licenca.DiasAntecedenciaRenovacaoPadrao,
            Status = StatusLicenca.Vigente
        };
    }

    [TestMethod]
    public async Task SelecionarPorIdAsync_e_SelecionarPorEmpreendimentoIdAsync_devem_respeitar_isolamento_por_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userIdA = Guid.NewGuid();
        var userIdB = Guid.NewGuid();
        var empreendimentoIdA = await CriarEmpreendimentoAsync(tenantA, userIdA);

        var repoTenantA = _fixture.CreateLicencaRepository(new TestUserContext
        {
            TenantId = tenantA,
            UserId = userIdA,
            Email = "tenant-a@ecotrack.test",
            Perfil = "Gestor"
        });

        var repoTenantB = _fixture.CreateLicencaRepository(new TestUserContext
        {
            TenantId = tenantB,
            UserId = userIdB,
            Email = "tenant-b@ecotrack.test",
            Perfil = "Gestor"
        });

        var licenca = CriarLicencaValida(tenantA, userIdA, empreendimentoIdA);
        await repoTenantA.CadastrarAsync(licenca, CancellationToken.None);

        var encontradaNoTenantA = await repoTenantA.SelecionarPorIdAsync(licenca.Id, CancellationToken.None);
        var encontradaNoTenantB = await repoTenantB.SelecionarPorIdAsync(licenca.Id, CancellationToken.None);
        var porEmpreendimentoNoTenantB = await repoTenantB.SelecionarPorEmpreendimentoIdAsync(empreendimentoIdA, CancellationToken.None);
        var existeNoTenantB = await repoTenantB.ExisteNumeroLicencaAsync(licenca.NumeroLicenca, cancellationToken: CancellationToken.None);

        Assert.IsNotNull(encontradaNoTenantA);
        Assert.AreEqual(licenca.Id, encontradaNoTenantA.Id);
        Assert.IsNull(encontradaNoTenantB);
        Assert.IsEmpty(porEmpreendimentoNoTenantB);
        Assert.IsFalse(existeNoTenantB);
    }

    [TestMethod]
    public async Task CadastrarAsync_e_EditarAsync_devem_atualizar_campos_e_retornar_true()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var empreendimentoId = await CriarEmpreendimentoAsync(tenantId, userId);

        var repo = _fixture.CreateLicencaRepository(new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "gestor@ecotrack.test",
            Perfil = "Admin"
        });

        var licenca = CriarLicencaValida(tenantId, userId, empreendimentoId, "LAO-2026-001", "IMA");
        await repo.CadastrarAsync(licenca, CancellationToken.None);

        var atualizada = CriarLicencaValida(tenantId, userId, empreendimentoId, "LAO-2026-001-REV", "IBAMA");
        atualizada.Id = licenca.Id;
        atualizada.Status = StatusLicenca.EmRenovacao;
        atualizada.NumeroProtocoloRenovacao = "PROT-REN-999";
        atualizada.DataProtocoloRenovacao = DateTime.UtcNow;

        var editou = await repo.EditarAsync(licenca.Id, atualizada, CancellationToken.None);
        var editouInexistente = await repo.EditarAsync(Guid.NewGuid(), atualizada, CancellationToken.None);
        var recuperada = await repo.SelecionarPorIdAsync(licenca.Id, CancellationToken.None);

        Assert.IsTrue(editou);
        Assert.IsFalse(editouInexistente);
        Assert.IsNotNull(recuperada);
        Assert.AreEqual("LAO-2026-001-REV", recuperada.NumeroLicenca);
        Assert.AreEqual("IBAMA", recuperada.OrgaoEmissor);
        Assert.AreEqual(StatusLicenca.EmRenovacao, recuperada.Status);
        Assert.AreEqual("PROT-REN-999", recuperada.NumeroProtocoloRenovacao);
    }

    [TestMethod]
    public async Task ExisteNumeroLicencaAsync_deve_considerar_ignorarId_corretamente()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var empreendimentoId = await CriarEmpreendimentoAsync(tenantId, userId);

        var repo = _fixture.CreateLicencaRepository(new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "gestor@ecotrack.test",
            Perfil = "Admin"
        });

        var numeroLicenca = $"LAP-{Guid.NewGuid().ToString("N")[..8]}";
        var licenca = CriarLicencaValida(tenantId, userId, empreendimentoId, numeroLicenca);
        await repo.CadastrarAsync(licenca, CancellationToken.None);

        var existeSemIgnorar = await repo.ExisteNumeroLicencaAsync(numeroLicenca, cancellationToken: CancellationToken.None);
        var existeIgnorandoPropria = await repo.ExisteNumeroLicencaAsync(numeroLicenca, licenca.Id, CancellationToken.None);
        var existeIgnorandoOutroId = await repo.ExisteNumeroLicencaAsync(numeroLicenca, Guid.NewGuid(), CancellationToken.None);

        Assert.IsTrue(existeSemIgnorar);
        Assert.IsFalse(existeIgnorandoPropria);
        Assert.IsTrue(existeIgnorandoOutroId);
    }

    [TestMethod]
    public async Task ExcluirAsync_deve_remover_registro_existente_e_retornar_false_se_inexistente()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var empreendimentoId = await CriarEmpreendimentoAsync(tenantId, userId);

        var repo = _fixture.CreateLicencaRepository(new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "admin@ecotrack.test",
            Perfil = "Admin"
        });

        var licenca = CriarLicencaValida(tenantId, userId, empreendimentoId);
        await repo.CadastrarAsync(licenca, CancellationToken.None);

        var excluiu = await repo.ExcluirAsync(licenca.Id, CancellationToken.None);
        var aposExclusao = await repo.SelecionarPorIdAsync(licenca.Id, CancellationToken.None);
        var segundoExcluir = await repo.ExcluirAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsTrue(excluiu);
        Assert.IsNull(aposExclusao);
        Assert.IsFalse(segundoExcluir);
    }
}