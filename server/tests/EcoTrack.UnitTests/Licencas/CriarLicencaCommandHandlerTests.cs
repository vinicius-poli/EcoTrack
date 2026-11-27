using EcoTrack.Application.Licencas;
using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Domain.Licencas;
using Moq;

namespace EcoTrack.UnitTests.Licencas;

[TestClass]
public sealed class CriarLicencaCommandHandlerTests
{
    private Mock<ILicencaRepository> _licencaRepositoryMock = null!;
    private Mock<IEmpreendimentoRepository> _empreendimentoRepositoryMock = null!;
    private Mock<IUserContext> _userContextMock = null!;
    private CriarLicencaCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _licencaRepositoryMock = new Mock<ILicencaRepository>();
        _empreendimentoRepositoryMock = new Mock<IEmpreendimentoRepository>();
        _userContextMock = new Mock<IUserContext>();
        _handler = new CriarLicencaCommandHandler(
            _licencaRepositoryMock.Object,
            _empreendimentoRepositoryMock.Object,
            _userContextMock.Object);
    }

    private static CriarLicencaRequest CriarRequestValido(Guid empreendimentoId)
    {
        var emissao = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        return new CriarLicencaRequest(
            EmpreendimentoId: empreendimentoId,
            NumeroProcesso: "  PROC-2026-300  ",
            NumeroLicenca: "  LAO-2026-077  ",
            OrgaoEmissor: "  ima  ",
            Tipo: TipoLicenca.LAO,
            DataEmissao: emissao,
            DataValidade: emissao.AddYears(2),
            DiasAntecedenciaRenovacao: null
        );
    }

    [TestMethod]
    public async Task Handle_QuandoTenantIdNulo_DeveRetornarFalhaENaoAcessarRepositorios()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns((Guid?)null);
        var command = new CriarLicencaCommand(CriarRequestValido(Guid.NewGuid()));

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
            r => r.CadastrarAsync(It.IsAny<Licenca>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosInvalidos_DeveRetornarFalhaENaoConsultarRepositorios()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.NewGuid());
        _userContextMock.SetupGet(u => u.UserId).Returns(Guid.NewGuid());

        var emissao = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var requestInvalido = new CriarLicencaRequest(
            EmpreendimentoId: Guid.Empty,
            NumeroProcesso: "",
            NumeroLicenca: " ",
            OrgaoEmissor: "",
            Tipo: TipoLicenca.LAP,
            DataEmissao: emissao,
            DataValidade: emissao.AddDays(-10),
            DiasAntecedenciaRenovacao: -1
        );
        var command = new CriarLicencaCommand(requestInvalido);

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
            r => r.CadastrarAsync(It.IsAny<Licenca>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoEmpreendimentoNaoExiste_DeveRetornarFalhaENaoCadastrar()
    {
        // Arrange
        var empreendimentoId = Guid.NewGuid();
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.NewGuid());
        _userContextMock.SetupGet(u => u.UserId).Returns(Guid.NewGuid());

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(empreendimentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Empreendimento?)null);

        var command = new CriarLicencaCommand(CriarRequestValido(empreendimentoId));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.ExisteNumeroLicencaAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _licencaRepositoryMock.Verify(
            r => r.CadastrarAsync(It.IsAny<Licenca>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoNumeroLicencaJaExiste_DeveRetornarFalhaENaoCadastrar()
    {
        // Arrange
        var empreendimentoId = Guid.NewGuid();
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.NewGuid());
        _userContextMock.SetupGet(u => u.UserId).Returns(Guid.NewGuid());

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(empreendimentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Empreendimento { Id = empreendimentoId });

        _licencaRepositoryMock
            .Setup(r => r.ExisteNumeroLicencaAsync("LAO-2026-077", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CriarLicencaCommand(CriarRequestValido(empreendimentoId));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.CadastrarAsync(It.IsAny<Licenca>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosValidos_DeveCadastrarComAntecedenciaPadraoENormalizacao()
    {
        // Arrange
        var empreendimentoId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Licenca? licencaCadastrada = null;

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);
        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(empreendimentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Empreendimento { Id = empreendimentoId });

        _licencaRepositoryMock
            .Setup(r => r.ExisteNumeroLicencaAsync("LAO-2026-077", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _licencaRepositoryMock
            .Setup(r => r.CadastrarAsync(It.IsAny<Licenca>(), It.IsAny<CancellationToken>()))
            .Callback<Licenca, CancellationToken>((lic, _) => licencaCadastrada = lic)
            .Returns(Task.CompletedTask);

        var command = new CriarLicencaCommand(CriarRequestValido(empreendimentoId));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsNotNull(licencaCadastrada);
        Assert.AreEqual(licencaCadastrada.Id, resultado.Value);
        Assert.AreEqual(tenantId, licencaCadastrada.TenantId);
        Assert.AreEqual(userId, licencaCadastrada.UserId);
        Assert.AreEqual("PROC-2026-300", licencaCadastrada.NumeroProcesso);
        Assert.AreEqual("LAO-2026-077", licencaCadastrada.NumeroLicenca);
        Assert.AreEqual("IMA", licencaCadastrada.OrgaoEmissor);
        Assert.AreEqual(Licenca.DiasAntecedenciaRenovacaoPadrao, licencaCadastrada.DiasAntecedenciaRenovacao);
        _licencaRepositoryMock.Verify(
            r => r.CadastrarAsync(It.IsAny<Licenca>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}