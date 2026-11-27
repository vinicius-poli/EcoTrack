using EcoTrack.Application.Empreendimentos;
using EcoTrack.Domain.Empreendimentos;
using Moq;

namespace EcoTrack.UnitTests.Empreendimentos;

[TestClass]
public sealed class AtualizarEmpreendimentoCommandHandlerTests
{
    private Mock<IEmpreendimentoRepository> _empreendimentoRepositoryMock = null!;
    private AtualizarEmpreendimentoCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _empreendimentoRepositoryMock = new Mock<IEmpreendimentoRepository>();
        _handler = new AtualizarEmpreendimentoCommandHandler(_empreendimentoRepositoryMock.Object);
    }

    private static Empreendimento CriarEmpreendimentoExistente(Guid id) => new()
    {
        Id = id,
        RazaoSocial = "Empresa Original Ltda",
        NomeFantasia = "Unidade Antiga",
        Cnpj = "45678912378952",
        Municipio = "Lages",
        Uf = "SC",
        Porte = PorteEmpreendimento.Pequeno,
        StatusLicenciamento = StatusLicenciamento.EmAnalise
    };

    private static AtualizarEmpreendimentoRequest CriarRequestValido() => new(
        RazaoSocial: "  Empresa Atualizada S/A  ",
        NomeFantasia: "  Unidade Atualizada  ",
        Cnpj: "12.345.678/0001-99",
        Municipio: "  Florianópolis  ",
        Uf: "sc",
        Porte: PorteEmpreendimento.Medio,
        StatusLicenciamento: StatusLicenciamento.Licenciado
    );

    [TestMethod]
    public async Task Handle_QuandoEmpreendimentoNaoExiste_DeveRetornarFalhaENaoValidarNemEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Empreendimento?)null);

        var command = new AtualizarEmpreendimentoCommand(id, CriarRequestValido());

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _empreendimentoRepositoryMock.Verify(
            r => r.ExisteCnpjAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _empreendimentoRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Guid>(), It.IsAny<Empreendimento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosInvalidos_DeveRetornarFalhaENaoConsultarCnpjNemEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existente = CriarEmpreendimentoExistente(id);

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        var requestInvalido = CriarRequestValido() with
        {
            RazaoSocial = "",
            Cnpj = "123",
            Uf = "S"
        };
        var command = new AtualizarEmpreendimentoCommand(id, requestInvalido);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _empreendimentoRepositoryMock.Verify(
            r => r.ExisteCnpjAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _empreendimentoRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Guid>(), It.IsAny<Empreendimento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoCnpjExisteEmOutroEmpreendimento_DeveRetornarFalhaENaoEditar()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existente = CriarEmpreendimentoExistente(id);

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        _empreendimentoRepositoryMock
            .Setup(r => r.ExisteCnpjAsync("12345678000199", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new AtualizarEmpreendimentoCommand(id, CriarRequestValido());

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _empreendimentoRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Guid>(), It.IsAny<Empreendimento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoEditarAsyncRetornaFalse_DeveRetornarFalha()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existente = CriarEmpreendimentoExistente(id);

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        _empreendimentoRepositoryMock
            .Setup(r => r.ExisteCnpjAsync("12345678000199", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _empreendimentoRepositoryMock
            .Setup(r => r.EditarAsync(id, existente, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new AtualizarEmpreendimentoCommand(id, CriarRequestValido());

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _empreendimentoRepositoryMock.Verify(
            r => r.EditarAsync(id, existente, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosValidos_DeveAtualizarNormalizarERetornarSucesso()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existente = CriarEmpreendimentoExistente(id);

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        _empreendimentoRepositoryMock
            .Setup(r => r.ExisteCnpjAsync("12345678000199", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _empreendimentoRepositoryMock
            .Setup(r => r.EditarAsync(id, existente, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new AtualizarEmpreendimentoCommand(id, CriarRequestValido());

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual("Empresa Atualizada S/A", existente.RazaoSocial);
        Assert.AreEqual("Unidade Atualizada", existente.NomeFantasia);
        Assert.AreEqual("12345678000199", existente.Cnpj);
        Assert.AreEqual("Florianópolis", existente.Municipio);
        Assert.AreEqual("SC", existente.Uf);
        Assert.AreEqual(PorteEmpreendimento.Medio, existente.Porte);
        Assert.AreEqual(StatusLicenciamento.Licenciado, existente.StatusLicenciamento);
        _empreendimentoRepositoryMock.Verify(
            r => r.EditarAsync(id, existente, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}