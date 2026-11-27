using EcoTrack.Application.Licencas;
using EcoTrack.Domain.Licencas;
using Moq;

namespace EcoTrack.UnitTests.Licencas;

[TestClass]
public sealed class ExcluirLicencaCommandHandlerTests
{
    private Mock<ILicencaRepository> _licencaRepositoryMock = null!;
    private ExcluirLicencaCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _licencaRepositoryMock = new Mock<ILicencaRepository>();
        _handler = new ExcluirLicencaCommandHandler(_licencaRepositoryMock.Object);
    }

    private static Licenca CriarLicenca(Guid id)
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
            Status = StatusLicenca.Vigente
        };
    }

    [TestMethod]
    public async Task Handle_QuandoLicencaNaoExiste_DeveRetornarFalhaENaoExcluir()
    {
        // Arrange
        var id = Guid.NewGuid();
        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licenca?)null);

        var command = new ExcluirLicencaCommand(id);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.ExcluirAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoExcluirAsyncRetornaFalse_DeveRetornarFalha()
    {
        // Arrange
        var id = Guid.NewGuid();
        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarLicenca(id));

        _licencaRepositoryMock
            .Setup(r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new ExcluirLicencaCommand(id);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoLicencaExisteEExclusaoSucede_DeveRetornarSucesso()
    {
        // Arrange
        var id = Guid.NewGuid();
        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarLicenca(id));

        _licencaRepositoryMock
            .Setup(r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new ExcluirLicencaCommand(id);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        _licencaRepositoryMock.Verify(
            r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}