using EcoTrack.Domain.Auth;
using EcoTrack.IntegrationTests.Infrastructure;
using Testcontainers.MsSql;

namespace EcoTrack.IntegrationTests.Repositories;

[TestClass]
public sealed class OrganizacaoRepositoryTests
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

    [TestMethod]
    public async Task AdicionarAsync_e_ObterPorIdAsync_deve_persistir_e_retornar_organizacao()
    {
        var userContext = new TestUserContext
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Email = "gestor@ecotrack.test",
            Perfil = "Admin"
        };

        var repository = _fixture.CreateOrganizacaoRepository(userContext);
        var organizacao = new Organizacao(
            Guid.NewGuid(),
            "Empresa Teste Ambiental",
            DateTime.UtcNow);

        await repository.AdicionarAsync(organizacao, CancellationToken.None);

        var recuperada = await repository.ObterPorIdAsync(organizacao.Id, CancellationToken.None);

        Assert.IsNotNull(recuperada);
        Assert.AreEqual(organizacao.Id, recuperada.Id);
        Assert.AreEqual("Empresa Teste Ambiental", recuperada.Nome);
    }
}