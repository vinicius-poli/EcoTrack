using EcoTrack.Application.Auth;
using EcoTrack.Domain.Auth;
using Moq;
using DomainUsuarioDto = EcoTrack.Domain.Auth.UsuarioDto;

namespace EcoTrack.UnitTests.Auth;

[TestClass]
public sealed class CadastrarMembroTenantCommandHandlerTests
{
    private Mock<IGerenciadorIdentidade> _identidadeMock = null!;
    private Mock<IUserContext> _userContextMock = null!;
    private Mock<IOrganizacaoRepository> _organizacaoRepositoryMock = null!;
    private CadastrarMembroTenantCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _identidadeMock = new Mock<IGerenciadorIdentidade>();
        _userContextMock = new Mock<IUserContext>();
        _organizacaoRepositoryMock = new Mock<IOrganizacaoRepository>();
        _handler = new CadastrarMembroTenantCommandHandler(
            _identidadeMock.Object,
            _userContextMock.Object,
            _organizacaoRepositoryMock.Object);
    }

    private static CadastrarMembroTenantCommand CriarCommandValido(string perfil = "gestor") =>
        new(
            Nome: "  Ana Carolina  ",
            Email: "  ana@ecotrack.com.br  ",
            Senha: "Senha@123",
            Perfil: perfil);

    [TestMethod]
    public async Task Handle_QuandoTenantIdNulo_DeveRetornarFalhaENaoAcessarDependencias()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns((Guid?)null);
        var command = CriarCommandValido();

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _identidadeMock.Verify(
            i => i.CadastrarAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _organizacaoRepositoryMock.Verify(
            r => r.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoTenantIdVazio_DeveRetornarFalhaENaoAcessarDependencias()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.Empty);
        var command = CriarCommandValido();

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _identidadeMock.Verify(
            i => i.CadastrarAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    [DataRow("", "ana@teste.com", "Senha@123", "Gestor")]
    [DataRow("   ", "ana@teste.com", "Senha@123", "Gestor")]
    [DataRow("Ana", "", "Senha@123", "Gestor")]
    [DataRow("Ana", "email-invalido", "Senha@123", "Gestor")]
    [DataRow("Ana", "ana@teste.com", "Curta1@", "Gestor")]
    [DataRow("Ana", "ana@teste.com", "semnumero@", "Gestor")]
    [DataRow("Ana", "ana@teste.com", "SemEspecial1", "Gestor")]
    [DataRow("Ana", "ana@teste.com", "Senha@123", "")]
    [DataRow("Ana", "ana@teste.com", "Senha@123", "   ")]
    [DataRow("Ana", "ana@teste.com", "Senha@123", "Administrador")]
    [DataRow("Ana", "ana@teste.com", "Senha@123", "Visitante")]
    public async Task Handle_QuandoDadosInvalidos_DeveRetornarFalhaENaoCadastrar(
        string nome,
        string email,
        string senha,
        string perfil)
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.NewGuid());
        var command = new CadastrarMembroTenantCommand(nome, email, senha, perfil);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _identidadeMock.Verify(
            i => i.CadastrarAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    [DataRow("gestor", nameof(PerfilUsuario.Gestor))]
    [DataRow("ANALISTA", nameof(PerfilUsuario.Analista))]
    public async Task Handle_QuandoDadosValidos_DeveCadastrarMembroENormalizarPerfil(
        string perfilEntrada,
        string perfilEsperado)
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var organizacao = new Organizacao(tenantId, "EcoTrack SC", DateTime.UtcNow);
        var command = CriarCommandValido(perfilEntrada);

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);

        _identidadeMock
            .Setup(i => i.CadastrarAsync(
                tenantId,
                "Ana Carolina",
                "ana@ecotrack.com.br",
                "Senha@123",
                perfilEsperado,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DomainUsuarioDto(
                usuarioId,
                tenantId,
                "Ana Carolina",
                "ana@ecotrack.com.br",
                perfilEsperado));

        _organizacaoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizacao);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess, string.Join(" | ", resultado.Errors.Select(e => e.Message)));
        Assert.AreEqual(usuarioId, resultado.Value.Id);
        Assert.AreEqual(tenantId, resultado.Value.TenantId);
        Assert.AreEqual("Ana Carolina", resultado.Value.Nome);
        Assert.AreEqual("ana@ecotrack.com.br", resultado.Value.Email);
        Assert.AreEqual(perfilEsperado, resultado.Value.Perfil);
        Assert.AreEqual("EcoTrack SC", resultado.Value.NomeOrganizacao);
    }

    [TestMethod]
    public async Task Handle_QuandoEmailDuplicado_DeveRetornarFalhaNoCampoEmail()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);

        _identidadeMock
            .Setup(i => i.CadastrarAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("EMAIL_DUPLICADO"));

        var command = CriarCommandValido();

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.IsTrue(resultado.Errors.Any(e => e.Message.Contains("e-mail", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task Handle_QuandoIdentidadeLancaArgumentException_DeveRetornarFalha()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);

        _identidadeMock
            .Setup(i => i.CadastrarAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Senha inválida."));

        var command = CriarCommandValido();

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.IsTrue(resultado.Errors.Any(e => e.Message.Contains("Senha inválida.")));
    }
}