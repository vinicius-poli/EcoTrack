using EcoTrack.Application.Licencas;
using EcoTrack.Domain.Licencas;
using Moq;

namespace EcoTrack.UnitTests.Licencas;

[TestClass]
public sealed class RegistrarProtocoloRenovacaoCommandHandlerTests
{
    private Mock<ILicencaRepository> _licencaRepositoryMock = null!;
    private RegistrarProtocoloRenovacaoCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _licencaRepositoryMock = new Mock<ILicencaRepository>();
        _handler = new RegistrarProtocoloRenovacaoCommandHandler(_licencaRepositoryMock.Object);
    }

    private static Licenca CriarLicenca(
        Guid id,
        StatusLicenca status = StatusLicenca.Vigente)
    {
        var emissao = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        return new Licenca
        {
            Id = id,
            EmpreendimentoId = Guid.NewGuid(),
            NumeroProcesso = "PROC-2026-010",
            NumeroLicenca = "LAO-2026-020",
            OrgaoEmissor = "IMA",
            Tipo = TipoLicenca.LAO,
            DataEmissao = emissao,
            DataValidade = emissao.AddYears(2),
            DiasAntecedenciaRenovacao = Licenca.DiasAntecedenciaRenovacaoPadrao,
            Status = status
        };
    }

    [TestMethod]
    public async Task Handle_QuandoLicencaNaoExiste_DeveRetornarFalhaENaoEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licenca?)null);

        var command = new RegistrarProtocoloRenovacaoCommand(
            id,
            new RegistrarProtocoloRenovacaoRequest("PROT-2026-001"));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Guid>(), It.IsAny<Licenca>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoRegraDeDominioFalha_DeveRetornarFalhaENaoEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        var licencaSuspensa = CriarLicenca(id, StatusLicenca.Suspensa);

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licencaSuspensa);

        var command = new RegistrarProtocoloRenovacaoCommand(
            id,
            new RegistrarProtocoloRenovacaoRequest("PROT-2026-001"));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Guid>(), It.IsAny<Licenca>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoEditarAsyncRetornaFalse_DeveRetornarFalha()
    {
        // Arrange
        var id = Guid.NewGuid();
        var licenca = CriarLicenca(id, StatusLicenca.Vigente);
        var dataProtocolo = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenca);

        _licencaRepositoryMock
            .Setup(r => r.EditarAsync(id, licenca, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new RegistrarProtocoloRenovacaoCommand(
            id,
            new RegistrarProtocoloRenovacaoRequest("PROT-2026-001", dataProtocolo));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.EditarAsync(id, licenca, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosValidos_DeveRegistrarProtocoloEditarERetornarSucesso()
    {
        // Arrange
        var id = Guid.NewGuid();
        var licenca = CriarLicenca(id, StatusLicenca.Vigente);
        var dataProtocolo = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenca);

        _licencaRepositoryMock
            .Setup(r => r.EditarAsync(id, licenca, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RegistrarProtocoloRenovacaoCommand(
            id,
            new RegistrarProtocoloRenovacaoRequest("  prot-2026-999  ", dataProtocolo));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(StatusLicenca.EmRenovacao, licenca.Status);
        Assert.AreEqual(dataProtocolo, licenca.DataProtocoloRenovacao);
        _licencaRepositoryMock.Verify(
            r => r.EditarAsync(id, licenca, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}