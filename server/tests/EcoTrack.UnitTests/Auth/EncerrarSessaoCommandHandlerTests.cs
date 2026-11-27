using EcoTrack.Application.Auth;
using EcoTrack.Domain.Auth;
using Moq;

namespace EcoTrack.UnitTests.Auth;

[TestClass]
public sealed class EncerrarSessaoCommandHandlerTests
{
    private Mock<IRevogacaoToken> _revogacaoMock = null!;
    private EncerrarSessaoCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _revogacaoMock = new Mock<IRevogacaoToken>();
        _handler = new EncerrarSessaoCommandHandler(_revogacaoMock.Object);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public async Task Handle_QuandoJtiInvalido_DeveRetornarFalhaENaoRevogarToken(string? jti)
    {
        // Arrange
        var command = new EncerrarSessaoCommand(jti!, DateTime.UtcNow.AddHours(1));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _revogacaoMock.Verify(
            r => r.RevogarAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoJtiValido_DeveRevogarTokenERetornarSucesso()
    {
        // Arrange
        const string jti = "jti-sessao-ativa-123";
        var expiraEmUtc = new DateTime(2026, 12, 1, 18, 0, 0, DateTimeKind.Utc);
        var command = new EncerrarSessaoCommand(jti, expiraEmUtc);

        _revogacaoMock
            .Setup(r => r.RevogarAsync(jti, expiraEmUtc, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess, string.Join(" | ", resultado.Errors.Select(e => e.Message)));
        _revogacaoMock.Verify(
            r => r.RevogarAsync(jti, expiraEmUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}