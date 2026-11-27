using EcoTrack.Application.Condicionantes;
using EcoTrack.Domain.Condicionantes;
using EcoTrack.Domain.Licencas;
using Moq;

namespace EcoTrack.UnitTests.Condicionantes;

[TestClass]
public sealed class AtualizarCondicionanteCommandHandlerTests
{
    private Mock<ICondicionanteRepository> _condicionanteRepositoryMock = null!;
    private Mock<ILicencaRepository> _licencaRepositoryMock = null!;
    private AtualizarCondicionanteCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _condicionanteRepositoryMock = new Mock<ICondicionanteRepository>();
        _licencaRepositoryMock = new Mock<ILicencaRepository>();
        _handler = new AtualizarCondicionanteCommandHandler(
            _condicionanteRepositoryMock.Object,
            _licencaRepositoryMock.Object);
    }

    private static Condicionante CriarCondicionanteExistente(Guid id, Guid tenantId, Guid licencaId)
    {
        return new Condicionante
        {
            Id = id,
            TenantId = tenantId,
            LicencaId = licencaId,
            CodigoItem = "COND-01",
            Titulo = "Título original",
            Descricao = "Descrição original",
            Periodicidade = PeriodicidadeCondicionante.Unica,
            DataVencimento = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            ResponsavelTecnico = "Eng. Original",
            Status = StatusCondicionante.Pendente
        };
    }

    private static AtualizarCondicionanteRequest CriarRequestValido(
        Guid licencaId,
        StatusCondicionante status = StatusCondicionante.EmAndamento)
    {
        return new AtualizarCondicionanteRequest(
            LicencaId: licencaId,
            CodigoItem: "  COND-05  ",
            Titulo: "  Título atualizado  ",
            Descricao: "  Descrição atualizada  ",
            Periodicidade: PeriodicidadeCondicionante.Semestral,
            DataVencimento: new DateTime(2027, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            ResponsavelTecnico: "  Eng. Responsável  ",
            Status: status
        );
    }

    [TestMethod]
    public async Task Handle_QuandoCondicionanteNaoExiste_DeveRetornarFalhaENaoEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Condicionante?)null);

        var command = new AtualizarCondicionanteCommand(id, CriarRequestValido(Guid.NewGuid()));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.SelecionarPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _condicionanteRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Condicionante>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoConcluirSemEvidencia_DeveRetornarFalhaENaoEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var licencaId = Guid.NewGuid();
        var existente = CriarCondicionanteExistente(id, tenantId, licencaId);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        var command = new AtualizarCondicionanteCommand(
            id,
            CriarRequestValido(licencaId, StatusCondicionante.Concluida));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.SelecionarPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _condicionanteRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Condicionante>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosInvalidos_DeveRetornarFalhaENaoConsultarLicencaNemEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var licencaId = Guid.NewGuid();
        var existente = CriarCondicionanteExistente(id, tenantId, licencaId);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        var requestInvalido = new AtualizarCondicionanteRequest(
            LicencaId: Guid.Empty,
            CodigoItem: "",
            Titulo: " ",
            Descricao: "",
            Periodicidade: PeriodicidadeCondicionante.Unica,
            DataVencimento: new DateTime(2027, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            ResponsavelTecnico: null,
            Status: StatusCondicionante.Pendente
        );
        var command = new AtualizarCondicionanteCommand(id, requestInvalido);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.SelecionarPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _condicionanteRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Condicionante>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoLicencaNaoExiste_DeveRetornarFalhaENaoEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var licencaId = Guid.NewGuid();
        var existente = CriarCondicionanteExistente(id, tenantId, licencaId);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(licencaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licenca?)null);

        var command = new AtualizarCondicionanteCommand(id, CriarRequestValido(licencaId));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _condicionanteRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Condicionante>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoEditarAsyncRetornaFalse_DeveRetornarFalha()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var licencaId = Guid.NewGuid();
        var existente = CriarCondicionanteExistente(id, tenantId, licencaId);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(licencaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Licenca { Id = licencaId });

        _condicionanteRepositoryMock
            .Setup(r => r.EditarAsync(existente, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new AtualizarCondicionanteCommand(id, CriarRequestValido(licencaId));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _condicionanteRepositoryMock.Verify(
            r => r.EditarAsync(existente, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosValidos_DeveAtualizarNormalizarERetornarSucesso()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var licencaId = Guid.NewGuid();
        var existente = CriarCondicionanteExistente(id, tenantId, licencaId);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(licencaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Licenca { Id = licencaId });

        _condicionanteRepositoryMock
            .Setup(r => r.EditarAsync(existente, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new AtualizarCondicionanteCommand(id, CriarRequestValido(licencaId));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual("COND-05", existente.CodigoItem);
        Assert.AreEqual("Título atualizado", existente.Titulo);
        Assert.AreEqual("Descrição atualizada", existente.Descricao);
        Assert.AreEqual(PeriodicidadeCondicionante.Semestral, existente.Periodicidade);
        Assert.AreEqual("Eng. Responsável", existente.ResponsavelTecnico);
        Assert.AreEqual(StatusCondicionante.EmAndamento, existente.Status);
        _condicionanteRepositoryMock.Verify(
            r => r.EditarAsync(existente, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}