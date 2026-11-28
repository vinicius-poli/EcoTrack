using EcoTrack.Domain.Empreendimentos;
using EcoTrack.IntegrationTests.Infrastructure;
using Testcontainers.MsSql;

namespace EcoTrack.IntegrationTests.Repositories;

[TestClass]
public sealed class EmpreendimentoRepositoryTests
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

    private static string GerarCnpjUnico() =>
        Random.Shared.NextInt64(10_000_000_000_000, 99_999_999_999_999).ToString("D14");

    private static Empreendimento CriarEmpreendimentoValido(
        Guid tenantId,
        Guid userId,
        string? cnpj = null,
        string municipio = "Rio do Sul",
        string uf = "SC")
    {
        var sufixo = Guid.NewGuid().ToString("N")[..6];

        return new Empreendimento
        {
            TenantId = tenantId,
            UserId = userId,
            RazaoSocial = $"Indústria {sufixo} Ltda",
            NomeFantasia = $"Unidade {sufixo}",
            Cnpj = cnpj ?? GerarCnpjUnico(),
            Municipio = municipio,
            Uf = uf,
            Porte = default,
            StatusLicenciamento = StatusLicenciamento.EmAnalise
        };
    }

    [TestMethod]
    public async Task SelecionarPorIdAsync_e_SelecionarPorCnpjAsync_devem_respeitar_isolamento_por_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userIdA = Guid.NewGuid();
        var userIdB = Guid.NewGuid();

        var repoTenantA = _fixture.CreateEmpreendimentoRepository(new TestUserContext
        {
            TenantId = tenantA,
            UserId = userIdA,
            Email = "tenant-a@ecotrack.test",
            Perfil = "Gestor"
        });

        var repoTenantB = _fixture.CreateEmpreendimentoRepository(new TestUserContext
        {
            TenantId = tenantB,
            UserId = userIdB,
            Email = "tenant-b@ecotrack.test",
            Perfil = "Gestor"
        });

        var empreendimento = CriarEmpreendimentoValido(tenantA, userIdA);
        await repoTenantA.CadastrarAsync(empreendimento, CancellationToken.None);

        var porIdA = await repoTenantA.SelecionarPorIdAsync(empreendimento.Id, CancellationToken.None);
        var porIdB = await repoTenantB.SelecionarPorIdAsync(empreendimento.Id, CancellationToken.None);
        var porCnpjB = await repoTenantB.SelecionarPorCnpjAsync(empreendimento.Cnpj, CancellationToken.None);
        var existeCnpjB = await repoTenantB.ExisteCnpjAsync(empreendimento.Cnpj, cancellationToken: CancellationToken.None);

        Assert.IsNotNull(porIdA);
        Assert.AreEqual(empreendimento.Id, porIdA.Id);
        Assert.IsNull(porIdB);
        Assert.IsNull(porCnpjB);
        Assert.IsFalse(existeCnpjB);
    }

    [TestMethod]
    public async Task CadastrarAsync_e_EditarAsync_devem_atualizar_campos_e_retornar_true()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var repo = _fixture.CreateEmpreendimentoRepository(new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "gestor@ecotrack.test",
            Perfil = "Admin"
        });

        var empreendimento = CriarEmpreendimentoValido(tenantId, userId, municipio: "Ituporanga", uf: "SC");
        await repo.CadastrarAsync(empreendimento, CancellationToken.None);

        var atualizado = CriarEmpreendimentoValido(tenantId, userId, cnpj: empreendimento.Cnpj, municipio: "Lages", uf: "SC");
        atualizado.Id = empreendimento.Id;
        atualizado.RazaoSocial = "Razão Social Atualizada S.A.";
        atualizado.NomeFantasia = "Nome Fantasia Atualizado";
        atualizado.StatusLicenciamento = StatusLicenciamento.Licenciado;

        var editou = await repo.EditarAsync(empreendimento.Id, atualizado, CancellationToken.None);
        var editouInexistente = await repo.EditarAsync(Guid.NewGuid(), atualizado, CancellationToken.None);
        var recuperado = await repo.SelecionarPorIdAsync(empreendimento.Id, CancellationToken.None);

        Assert.IsTrue(editou);
        Assert.IsFalse(editouInexistente);
        Assert.IsNotNull(recuperado);
        Assert.AreEqual("Razão Social Atualizada S.A.", recuperado.RazaoSocial);
        Assert.AreEqual("Nome Fantasia Atualizado", recuperado.NomeFantasia);
        Assert.AreEqual("Lages", recuperado.Municipio);
        Assert.AreEqual(StatusLicenciamento.Licenciado, recuperado.StatusLicenciamento);
    }

    [TestMethod]
    public async Task ExisteCnpjAsync_e_FiltrarAsync_devem_respeitar_ignorarId_e_criterio()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var repo = _fixture.CreateEmpreendimentoRepository(new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "gestor@ecotrack.test",
            Perfil = "Admin"
        });

        var cnpjAlvo = GerarCnpjUnico();
        var empAlvo = CriarEmpreendimentoValido(tenantId, userId, cnpj: cnpjAlvo, municipio: "Trombudo Central");
        var empOutro = CriarEmpreendimentoValido(tenantId, userId, municipio: "Rio do Sul");

        await repo.CadastrarAsync(empAlvo, CancellationToken.None);
        await repo.CadastrarAsync(empOutro, CancellationToken.None);

        var existeSemIgnorar = await repo.ExisteCnpjAsync(cnpjAlvo, cancellationToken: CancellationToken.None);
        var existeIgnorandoProprio = await repo.ExisteCnpjAsync(cnpjAlvo, empAlvo.Id, CancellationToken.None);
        var filtradosPorMunicipio = await repo.FiltrarAsync(e => e.Municipio == "Trombudo Central", CancellationToken.None);

        Assert.IsTrue(existeSemIgnorar);
        Assert.IsFalse(existeIgnorandoProprio);
        Assert.HasCount(1, filtradosPorMunicipio);
        Assert.AreEqual(empAlvo.Id, filtradosPorMunicipio[0].Id);
    }

    [TestMethod]
    public async Task ExcluirAsync_deve_remover_registro_existente_e_retornar_false_se_inexistente()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var repo = _fixture.CreateEmpreendimentoRepository(new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "admin@ecotrack.test",
            Perfil = "Admin"
        });

        var empreendimento = CriarEmpreendimentoValido(tenantId, userId);
        await repo.CadastrarAsync(empreendimento, CancellationToken.None);

        var excluiu = await repo.ExcluirAsync(empreendimento.Id, CancellationToken.None);
        var aposExclusao = await repo.SelecionarPorIdAsync(empreendimento.Id, CancellationToken.None);
        var segundoExcluir = await repo.ExcluirAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsTrue(excluiu);
        Assert.IsNull(aposExclusao);
        Assert.IsFalse(segundoExcluir);
    }
}