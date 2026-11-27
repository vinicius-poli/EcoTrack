using EcoTrack.Application.Licencas;
using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Domain.Licencas;
using Moq;

namespace EcoTrack.UnitTests.Licencas;

[TestClass]
public sealed class AtualizarLicencaCommandHandlerTests
{
    private Mock<ILicencaRepository> _licencaRepositoryMock = null!;
    private Mock<IEmpreendimentoRepository> _empreendimentoRepositoryMock = null!;
    private AtualizarLicencaCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _licencaRepositoryMock = new Mock<ILicencaRepository>();
        _empreendimentoRepositoryMock = new Mock<IEmpreendimentoRepository>();
        _handler = new AtualizarLicencaCommandHandler(
            _licencaRepositoryMock.Object,
            _empreendimentoRepositoryMock.Object);
    }

    private static Licenca CriarLicencaExistente(
        Guid id,
        Guid empreendimentoId,
        StatusLicenca status = StatusLicenca.Vigente)
    {
        var emissao = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        return new Licenca
        {
            Id = id,
            EmpreendimentoId = empreendimentoId,
            NumeroProcesso = "PROC-2026-001",
            NumeroLicenca = "LAO-2026-001",
            OrgaoEmissor = "IMA",
            Tipo = TipoLicenca.LAO,
            DataEmissao = emissao,
            DataValidade = emissao.AddYears(2),
            DiasAntecedenciaRenovacao = Licenca.DiasAntecedenciaRenovacaoPadrao,
            Status = status
        };
    }

    private static AtualizarLicencaRequest CriarRequestValido(Guid empreendimentoId)
    {
        var emissao = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        return new AtualizarLicencaRequest(
            EmpreendimentoId: empreendimentoId,
            NumeroProcesso: "  PROC-2026-400  ",
            NumeroLicenca: "  LAI-2026-088  ",
            OrgaoEmissor: "  ima  ",
            Tipo: TipoLicenca.LAI,
            DataEmissao: emissao,
            DataValidade: emissao.AddYears(1),
            DiasAntecedenciaRenovacao: null,
            Status: StatusLicenca.Vigente
        );
    }

    [TestMethod]
    public async Task Handle_QuandoLicencaNaoExiste_DeveRetornarFalhaENaoEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licenca?)null);

        var command = new AtualizarLicencaCommand(id, CriarRequestValido(Guid.NewGuid()));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _empreendimentoRepositoryMock.Verify(
            r => r.SelecionarPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _licencaRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Guid>(), It.IsAny<Licenca>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoTentaEntrarEmRenovacaoSemProtocolo_DeveRetornarFalhaENaoEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        var empreendimentoId = Guid.NewGuid();
        var licencaExistente = CriarLicencaExistente(id, empreendimentoId, StatusLicenca.Vigente);

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licencaExistente);

        var request = CriarRequestValido(empreendimentoId) with
        {
            Status = StatusLicenca.EmRenovacao
        };
        var command = new AtualizarLicencaCommand(id, request);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(StatusLicenca.Vigente, licencaExistente.Status);
        _licencaRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Guid>(), It.IsAny<Licenca>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosInvalidos_DeveRetornarFalhaENaoConsultarDependenciasNemEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        var empreendimentoId = Guid.NewGuid();
        var licencaExistente = CriarLicencaExistente(id, empreendimentoId);
        var emissao = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licencaExistente);

        var requestInvalido = new AtualizarLicencaRequest(
            EmpreendimentoId: Guid.Empty,
            NumeroProcesso: "",
            NumeroLicenca: "",
            OrgaoEmissor: "",
            Tipo: TipoLicenca.LAP,
            DataEmissao: emissao,
            DataValidade: emissao.AddDays(-5),
            DiasAntecedenciaRenovacao: -10,
            Status: StatusLicenca.Vigente
        );
        var command = new AtualizarLicencaCommand(id, requestInvalido);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _empreendimentoRepositoryMock.Verify(
            r => r.SelecionarPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _licencaRepositoryMock.Verify(
            r => r.ExisteNumeroLicencaAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _licencaRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Guid>(), It.IsAny<Licenca>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoEmpreendimentoNaoExiste_DeveRetornarFalhaENaoEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        var empreendimentoId = Guid.NewGuid();
        var licencaExistente = CriarLicencaExistente(id, empreendimentoId);

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licencaExistente);

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(empreendimentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Empreendimento?)null);

        var command = new AtualizarLicencaCommand(id, CriarRequestValido(empreendimentoId));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.ExisteNumeroLicencaAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _licencaRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Guid>(), It.IsAny<Licenca>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoNumeroLicencaJaExisteEmOutraLicenca_DeveRetornarFalhaENaoEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        var empreendimentoId = Guid.NewGuid();
        var licencaExistente = CriarLicencaExistente(id, empreendimentoId);

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licencaExistente);

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(empreendimentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Empreendimento { Id = empreendimentoId });

        _licencaRepositoryMock
            .Setup(r => r.ExisteNumeroLicencaAsync("LAI-2026-088", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new AtualizarLicencaCommand(id, CriarRequestValido(empreendimentoId));

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
        var empreendimentoId = Guid.NewGuid();
        var licencaExistente = CriarLicencaExistente(id, empreendimentoId);

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licencaExistente);

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(empreendimentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Empreendimento { Id = empreendimentoId });

        _licencaRepositoryMock
            .Setup(r => r.ExisteNumeroLicencaAsync("LAI-2026-088", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _licencaRepositoryMock
            .Setup(r => r.EditarAsync(id, licencaExistente, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new AtualizarLicencaCommand(id, CriarRequestValido(empreendimentoId));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.EditarAsync(id, licencaExistente, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosValidos_DeveAtualizarNormalizarERetornarSucesso()
    {
        // Arrange
        var id = Guid.NewGuid();
        var empreendimentoId = Guid.NewGuid();
        var licencaExistente = CriarLicencaExistente(id, empreendimentoId);

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licencaExistente);

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(empreendimentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Empreendimento { Id = empreendimentoId });

        _licencaRepositoryMock
            .Setup(r => r.ExisteNumeroLicencaAsync("LAI-2026-088", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _licencaRepositoryMock
            .Setup(r => r.EditarAsync(id, licencaExistente, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new AtualizarLicencaCommand(id, CriarRequestValido(empreendimentoId));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual("PROC-2026-400", licencaExistente.NumeroProcesso);
        Assert.AreEqual("LAI-2026-088", licencaExistente.NumeroLicenca);
        Assert.AreEqual("IMA", licencaExistente.OrgaoEmissor);
        Assert.AreEqual(TipoLicenca.LAI, licencaExistente.Tipo);
        Assert.AreEqual(Licenca.DiasAntecedenciaRenovacaoPadrao, licencaExistente.DiasAntecedenciaRenovacao);
        Assert.AreEqual(StatusLicenca.Vigente, licencaExistente.Status);
        _licencaRepositoryMock.Verify(
            r => r.EditarAsync(id, licencaExistente, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}