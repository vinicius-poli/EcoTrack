using EcoTrack.Application.Auth;
using EcoTrack.Domain.Auth;
using Moq;
using DomainUsuarioDto = EcoTrack.Domain.Auth.UsuarioDto;

namespace EcoTrack.UnitTests.Auth;

[TestClass]
public sealed class CadastrarUsuarioCommandHandlerTests
{
    private Mock<IGerenciadorIdentidade> _identidadeMock = null!;
    private Mock<IOrganizacaoRepository> _organizacaoRepositoryMock = null!;
    private CadastrarUsuarioCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _identidadeMock = new Mock<IGerenciadorIdentidade>();
        _organizacaoRepositoryMock = new Mock<IOrganizacaoRepository>();
        _handler = new CadastrarUsuarioCommandHandler(
            _identidadeMock.Object,
            _organizacaoRepositoryMock.Object);
    }

    private static CadastrarUsuarioCommand CriarCommandValido() =>
        new(
            Nome: "  Vinícius Silva  ",
            Email: "  vinicius@ecotrack.com.br  ",
            Senha: "Senha@123",
            NomeOrganizacao: "  EcoTrack Ambiental  ");

    [TestMethod]
    [DataRow("", "eco@teste.com", "Senha@123", "Org")]
    [DataRow("   ", "eco@teste.com", "Senha@123", "Org")]
    [DataRow("Vinícius", "", "Senha@123", "Org")]
    [DataRow("Vinícius", "email-invalido", "Senha@123", "Org")]
    [DataRow("Vinícius", "eco@teste.com", "Curta1@", "Org")]
    [DataRow("Vinícius", "eco@teste.com", "semnumero@", "Org")]
    [DataRow("Vinícius", "eco@teste.com", "SemEspecial1", "Org")]
    [DataRow("Vinícius", "eco@teste.com", "Senha@123", "")]
    [DataRow("Vinícius", "eco@teste.com", "Senha@123", "   ")]
    public async Task Handle_QuandoDadosInvalidos_DeveRetornarFalhaENaoPersistir(
        string nome,
        string email,
        string senha,
        string nomeOrganizacao)
    {
        // Arrange
        var command = new CadastrarUsuarioCommand(nome, email, senha, nomeOrganizacao);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _organizacaoRepositoryMock.Verify(
            r => r.AdicionarAsync(It.IsAny<Organizacao>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
    public async Task Handle_QuandoDadosValidos_DeveCriarOrganizacaoCadastrarAdministradorERetornarDto()
    {
        // Arrange
        var command = CriarCommandValido();
        var usuarioId = Guid.NewGuid();
        Guid tenantCapturado = Guid.Empty;

        _organizacaoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Organizacao>(), It.IsAny<CancellationToken>()))
            .Callback<Organizacao, CancellationToken>((org, _) => tenantCapturado = org.Id)
            .Returns(Task.CompletedTask);

        _identidadeMock
            .Setup(i => i.CadastrarAsync(
                It.IsAny<Guid>(),
                "Vinícius Silva",
                "vinicius@ecotrack.com.br",
                "Senha@123",
                nameof(PerfilUsuario.Administrador),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid tId, string nome, string email, string _, string perfil, CancellationToken _) =>
                new DomainUsuarioDto(usuarioId, tId, nome, email, perfil));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess, string.Join(" | ", resultado.Errors.Select(e => e.Message)));
        Assert.AreNotEqual(Guid.Empty, tenantCapturado);
        Assert.AreEqual(usuarioId, resultado.Value.Id);
        Assert.AreEqual(tenantCapturado, resultado.Value.TenantId);
        Assert.AreEqual("Vinícius Silva", resultado.Value.Nome);
        Assert.AreEqual("vinicius@ecotrack.com.br", resultado.Value.Email);
        Assert.AreEqual(nameof(PerfilUsuario.Administrador), resultado.Value.Perfil);
        Assert.AreEqual("EcoTrack Ambiental", resultado.Value.NomeOrganizacao);

        _organizacaoRepositoryMock.Verify(
            r => r.AdicionarAsync(
                It.Is<Organizacao>(o => o.Id == tenantCapturado && o.Nome == "EcoTrack Ambiental"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _identidadeMock.Verify(
            i => i.CadastrarAsync(
                tenantCapturado,
                "Vinícius Silva",
                "vinicius@ecotrack.com.br",
                "Senha@123",
                nameof(PerfilUsuario.Administrador),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoEmailDuplicado_DeveRetornarFalhaNoCampoEmail()
    {
        // Arrange
        var command = CriarCommandValido();

        _organizacaoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Organizacao>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _identidadeMock
            .Setup(i => i.CadastrarAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("EMAIL_DUPLICADO"));

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
        var command = CriarCommandValido();

        _organizacaoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Organizacao>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _identidadeMock
            .Setup(i => i.CadastrarAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Senha inválida pela política."));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.IsTrue(resultado.Errors.Any(e => e.Message.Contains("Senha inválida pela política.")));
    }
}