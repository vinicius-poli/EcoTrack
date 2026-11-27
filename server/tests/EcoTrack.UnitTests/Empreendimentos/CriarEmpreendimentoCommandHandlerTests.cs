using EcoTrack.Application.Empreendimentos;
using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Empreendimentos;
using Moq;

namespace EcoTrack.UnitTests.Empreendimentos;

[TestClass]
public sealed class CriarEmpreendimentoCommandHandlerTests
{
    private Mock<IEmpreendimentoRepository> _empreendimentoRepositoryMock = null!;
    private Mock<IUserContext> _userContextMock = null!;
    private CriarEmpreendimentoCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _empreendimentoRepositoryMock = new Mock<IEmpreendimentoRepository>();
        _userContextMock = new Mock<IUserContext>();
        _handler = new CriarEmpreendimentoCommandHandler(
            _empreendimentoRepositoryMock.Object,
            _userContextMock.Object);
    }

    private static CriarEmpreendimentoRequest CriarRequestValido() => new(
        RazaoSocial: "Papel e Celulose Serra Ltda",
        NomeFantasia: "Unidade Lages",
        Cnpj: "45.678.912/3789-52",
        Municipio: "Lages",
        Uf: "sc",
        Porte: PorteEmpreendimento.Medio,
        StatusLicenciamento: StatusLicenciamento.Licenciado
    );

    [TestMethod]
    public async Task Handle_QuandoTenantIdNulo_DeveRetornarFalhaENaoAcessarRepositorio()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns((Guid?)null);
        var command = new CriarEmpreendimentoCommand(CriarRequestValido());

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _empreendimentoRepositoryMock.Verify(
            r => r.ExisteCnpjAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _empreendimentoRepositoryMock.Verify(
            r => r.CadastrarAsync(It.IsAny<Empreendimento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosInvalidos_DeveRetornarFalhaENaoCadastrar()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.NewGuid());
        _userContextMock.SetupGet(u => u.UserId).Returns(Guid.NewGuid());

        var requestInvalido = CriarRequestValido() with
        {
            RazaoSocial = "",
            Cnpj = "123",
            Uf = "S"
        };
        var command = new CriarEmpreendimentoCommand(requestInvalido);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _empreendimentoRepositoryMock.Verify(
            r => r.ExisteCnpjAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _empreendimentoRepositoryMock.Verify(
            r => r.CadastrarAsync(It.IsAny<Empreendimento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoCnpjJaExiste_DeveRetornarFalhaENaoCadastrar()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.NewGuid());
        _userContextMock.SetupGet(u => u.UserId).Returns(Guid.NewGuid());

        _empreendimentoRepositoryMock
            .Setup(r => r.ExisteCnpjAsync("45678912378952", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CriarEmpreendimentoCommand(CriarRequestValido());

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _empreendimentoRepositoryMock.Verify(
            r => r.CadastrarAsync(It.IsAny<Empreendimento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosValidosECnpjUnico_DeveCadastrarERetornarId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Empreendimento? entidadeCadastrada = null;

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);
        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        _empreendimentoRepositoryMock
            .Setup(r => r.ExisteCnpjAsync("45678912378952", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _empreendimentoRepositoryMock
            .Setup(r => r.CadastrarAsync(It.IsAny<Empreendimento>(), It.IsAny<CancellationToken>()))
            .Callback<Empreendimento, CancellationToken>((emp, _) => entidadeCadastrada = emp)
            .Returns(Task.CompletedTask);

        var command = new CriarEmpreendimentoCommand(CriarRequestValido());

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsNotNull(entidadeCadastrada);
        Assert.AreEqual(entidadeCadastrada.Id, resultado.Value);
        Assert.AreEqual(tenantId, entidadeCadastrada.TenantId);
        Assert.AreEqual(userId, entidadeCadastrada.UserId);
        Assert.AreEqual("45678912378952", entidadeCadastrada.Cnpj);
        Assert.AreEqual("SC", entidadeCadastrada.Uf);
        _empreendimentoRepositoryMock.Verify(
            r => r.CadastrarAsync(It.IsAny<Empreendimento>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}