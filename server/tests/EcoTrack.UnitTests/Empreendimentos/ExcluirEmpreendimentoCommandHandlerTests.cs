using EcoTrack.Application.Empreendimentos;
using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Domain.Licencas;
using Moq;

namespace EcoTrack.UnitTests.Empreendimentos;

[TestClass]
public sealed class ExcluirEmpreendimentoCommandHandlerTests
{
    private Mock<IEmpreendimentoRepository> _empreendimentoRepositoryMock = null!;
    private Mock<ILicencaRepository> _licencaRepositoryMock = null!;
    private ExcluirEmpreendimentoCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _empreendimentoRepositoryMock = new Mock<IEmpreendimentoRepository>();
        _licencaRepositoryMock = new Mock<ILicencaRepository>();
        _handler = new ExcluirEmpreendimentoCommandHandler(
            _empreendimentoRepositoryMock.Object,
            _licencaRepositoryMock.Object);
    }

    private static Empreendimento CriarEmpreendimento(Guid id) => new()
    {
        Id = id,
        RazaoSocial = "Empresa Teste Ltda",
        NomeFantasia = "Unidade Teste",
        Cnpj = "45678912378952",
        Municipio = "Lages",
        Uf = "SC",
        Porte = PorteEmpreendimento.Pequeno,
        StatusLicenciamento = StatusLicenciamento.EmAnalise
    };

    [TestMethod]
    public async Task Handle_QuandoEmpreendimentoNaoExiste_DeveRetornarFalhaENaoExcluir()
    {
        // Arrange
        var id = Guid.NewGuid();
        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Empreendimento?)null);

        var command = new ExcluirEmpreendimentoCommand(id);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.SelecionarPorEmpreendimentoIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _empreendimentoRepositoryMock.Verify(
            r => r.ExcluirAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoExistemLicencasVinculadas_DeveRetornarFalhaENaoExcluir()
    {
        // Arrange
        var id = Guid.NewGuid();
        var licencas = new List<Licenca>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EmpreendimentoId = id,
                NumeroProcesso = "PROC-2026-001",
                NumeroLicenca = "LAO-2026-001",
                OrgaoEmissor = "IMA",
                Tipo = TipoLicenca.LAO
            }
        };

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarEmpreendimento(id));

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorEmpreendimentoIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licencas);

        var command = new ExcluirEmpreendimentoCommand(id);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _empreendimentoRepositoryMock.Verify(
            r => r.ExcluirAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoExcluirAsyncRetornaFalse_DeveRetornarFalha()
    {
        // Arrange
        var id = Guid.NewGuid();

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarEmpreendimento(id));

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorEmpreendimentoIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _empreendimentoRepositoryMock
            .Setup(r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new ExcluirEmpreendimentoCommand(id);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _empreendimentoRepositoryMock.Verify(
            r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoSemLicencasVinculadas_DeveExcluirERetornarSucesso()
    {
        // Arrange
        var id = Guid.NewGuid();

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarEmpreendimento(id));

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorEmpreendimentoIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _empreendimentoRepositoryMock
            .Setup(r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new ExcluirEmpreendimentoCommand(id);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        _empreendimentoRepositoryMock.Verify(
            r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}