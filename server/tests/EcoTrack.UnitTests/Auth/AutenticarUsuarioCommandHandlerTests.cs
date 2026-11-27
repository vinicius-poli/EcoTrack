using EcoTrack.Application.Auth;
using EcoTrack.Domain.Auth;
using Moq;
using DomainTokenDto = EcoTrack.Domain.Auth.TokenDto;

namespace EcoTrack.UnitTests.Auth;

[TestClass]
public sealed class AutenticarUsuarioCommandHandlerTests
{
    private Mock<IGerenciadorIdentidade> _identidadeMock = null!;
    private Mock<IEmissorTokens> _emissorTokensMock = null!;
    private AutenticarUsuarioCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _identidadeMock = new Mock<IGerenciadorIdentidade>();
        _emissorTokensMock = new Mock<IEmissorTokens>();
        _handler = new AutenticarUsuarioCommandHandler(
            _identidadeMock.Object,
            _emissorTokensMock.Object);
    }

    [TestMethod]
    [DataRow("", "Senha@123")]
    [DataRow("   ", "Senha@123")]
    [DataRow(null, "Senha@123")]
    [DataRow("vinicius@ecotrack.com.br", "")]
    [DataRow("vinicius@ecotrack.com.br", "   ")]
    [DataRow("vinicius@ecotrack.com.br", null)]
    public async Task Handle_QuandoEmailOuSenhaAusentes_DeveRetornarFalhaENaoAutenticar(
        string? email,
        string? senha)
    {
        // Arrange
        var command = new AutenticarUsuarioCommand(email, senha);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _identidadeMock.Verify(
            i => i.AutenticarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _emissorTokensMock.Verify(
            e => e.GerarToken(It.IsAny<CredenciaisUsuario>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoCredenciaisInvalidas_DeveRetornarFalhaENaoGerarToken()
    {
        // Arrange
        var command = new AutenticarUsuarioCommand("vinicius@ecotrack.com.br", "SenhaErrada@123");

        _identidadeMock
            .Setup(i => i.AutenticarAsync(
                "vinicius@ecotrack.com.br",
                "SenhaErrada@123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CredenciaisUsuario?)null);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _emissorTokensMock.Verify(
            e => e.GerarToken(It.IsAny<CredenciaisUsuario>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoCredenciaisValidas_DeveAutenticarGerarTokenERetornarDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var expiracaoUtc = DateTime.UtcNow.AddHours(2);
        const string accessToken = "jwt-token-valido-ecotrack";

        var command = new AutenticarUsuarioCommand("vinicius@ecotrack.com.br", "Senha@123");
        var credenciais = new CredenciaisUsuario(
            Id: userId,
            TenantId: tenantId,
            Nome: "Vinícius Silva",
            Email: "vinicius@ecotrack.com.br",
            Perfil: nameof(PerfilUsuario.Administrador),
            NomeOrganizacao: "EcoTrack Ambiental");

        var tokenDomain = new DomainTokenDto(
            UserId: userId,
            TenantId: tenantId,
            Nome: credenciais.Nome,
            Email: credenciais.Email,
            Perfil: credenciais.Perfil,
            AccessToken: accessToken,
            DataExpiracaoEmUtc: expiracaoUtc);

        _identidadeMock
            .Setup(i => i.AutenticarAsync(
                "vinicius@ecotrack.com.br",
                "Senha@123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(credenciais);

        _emissorTokensMock
            .Setup(e => e.GerarToken(credenciais))
            .Returns(tokenDomain);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess, string.Join(" | ", resultado.Errors.Select(e => e.Message)));
        Assert.AreEqual(accessToken, resultado.Value.Token);

        _identidadeMock.Verify(
            i => i.AutenticarAsync(
                "vinicius@ecotrack.com.br",
                "Senha@123",
                It.IsAny<CancellationToken>()),
            Times.Once);
        _emissorTokensMock.Verify(
            e => e.GerarToken(credenciais),
            Times.Once);
    }
}