using EcoTrack.Application.Condicionantes;
using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Condicionantes;
using EcoTrack.Domain.Licencas;
using Moq;

namespace EcoTrack.UnitTests.Condicionantes;

[TestClass]
public sealed class CriarCondicionanteCommandHandlerTests
{
    private Mock<ICondicionanteRepository> _condicionanteRepositoryMock = null!;
    private Mock<ILicencaRepository> _licencaRepositoryMock = null!;
    private Mock<IUserContext> _userContextMock = null!;
    private CriarCondicionanteCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _condicionanteRepositoryMock = new Mock<ICondicionanteRepository>();
        _licencaRepositoryMock = new Mock<ILicencaRepository>();
        _userContextMock = new Mock<IUserContext>();
        _handler = new CriarCondicionanteCommandHandler(
            _condicionanteRepositoryMock.Object,
            _licencaRepositoryMock.Object,
            _userContextMock.Object);
    }

    private static CriarCondicionanteRequest CriarRequestValido(Guid licencaId)
    {
        return new CriarCondicionanteRequest(
            LicencaId: licencaId,
            CodigoItem: "  COND-03  ",
            Titulo: "  Plano de gerenciamento de resíduos  ",
            Descricao: "  Enviar relatório trimestral de destinação.  ",
            Periodicidade: PeriodicidadeCondicionante.Trimestral,
            DataVencimento: new DateTime(2026, 12, 20, 0, 0, 0, DateTimeKind.Utc),
            ResponsavelTecnico: "  Eng. Ambiental  "
        );
    }

    [TestMethod]
    public async Task Handle_QuandoTenantIdNulo_DeveRetornarFalhaENaoAcessarRepositorios()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns((Guid?)null);
        var command = new CriarCondicionanteCommand(CriarRequestValido(Guid.NewGuid()));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.SelecionarPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _condicionanteRepositoryMock.Verify(
            r => r.CadastrarAsync(It.IsAny<Condicionante>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosInvalidos_DeveRetornarFalhaENaoConsultarLicencaNemCadastrar()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.NewGuid());
        _userContextMock.SetupGet(u => u.UserId).Returns(Guid.NewGuid());

        var requestInvalido = new CriarCondicionanteRequest(
            LicencaId: Guid.Empty,
            CodigoItem: "",
            Titulo: " ",
            Descricao: "",
            Periodicidade: PeriodicidadeCondicionante.Unica,
            DataVencimento: new DateTime(2026, 12, 20, 0, 0, 0, DateTimeKind.Utc),
            ResponsavelTecnico: null
        );
        var command = new CriarCondicionanteCommand(requestInvalido);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.SelecionarPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _condicionanteRepositoryMock.Verify(
            r => r.CadastrarAsync(It.IsAny<Condicionante>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoLicencaNaoExiste_DeveRetornarFalhaENaoCadastrar()
    {
        // Arrange
        var licencaId = Guid.NewGuid();
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.NewGuid());
        _userContextMock.SetupGet(u => u.UserId).Returns(Guid.NewGuid());

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(licencaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licenca?)null);

        var command = new CriarCondicionanteCommand(CriarRequestValido(licencaId));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _condicionanteRepositoryMock.Verify(
            r => r.CadastrarAsync(It.IsAny<Condicionante>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosValidos_DeveCadastrarNormalizarERetornarId()
    {
        // Arrange
        var licencaId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Condicionante? condicionanteCadastrada = null;

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);
        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(licencaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Licenca { Id = licencaId });

        _condicionanteRepositoryMock
            .Setup(r => r.CadastrarAsync(It.IsAny<Condicionante>(), It.IsAny<CancellationToken>()))
            .Callback<Condicionante, CancellationToken>((cond, _) => condicionanteCadastrada = cond)
            .Returns(Task.CompletedTask);

        var command = new CriarCondicionanteCommand(CriarRequestValido(licencaId));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsNotNull(condicionanteCadastrada);
        Assert.AreEqual(condicionanteCadastrada.Id, resultado.Value);
        Assert.AreEqual(tenantId, condicionanteCadastrada.TenantId);
        Assert.AreEqual(userId, condicionanteCadastrada.UserId);
        Assert.AreEqual(StatusCondicionante.Pendente, condicionanteCadastrada.Status);
        Assert.AreEqual("COND-03", condicionanteCadastrada.CodigoItem);
        Assert.AreEqual("Plano de gerenciamento de resíduos", condicionanteCadastrada.Titulo);
        Assert.AreEqual("Enviar relatório trimestral de destinação.", condicionanteCadastrada.Descricao);
        Assert.AreEqual("Eng. Ambiental", condicionanteCadastrada.ResponsavelTecnico);
        _condicionanteRepositoryMock.Verify(
            r => r.CadastrarAsync(It.IsAny<Condicionante>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}