using EcoTrack.Application.Auth;
using EcoTrack.Domain.Auth;
using Moq;
using DomainUsuarioDto = EcoTrack.Domain.Auth.UsuarioDto;

namespace EcoTrack.UnitTests.Auth;

[TestClass]
public sealed class ObterUsuarioQueryHandlerTests
{
    private Mock<IGerenciadorIdentidade> _identidadeMock = null!;
    private Mock<IOrganizacaoRepository> _organizacaoRepositoryMock = null!;
    private ObterUsuarioQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _identidadeMock = new Mock<IGerenciadorIdentidade>();
        _organizacaoRepositoryMock = new Mock<IOrganizacaoRepository>();
        _handler = new ObterUsuarioQueryHandler(
            _identidadeMock.Object,
            _organizacaoRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_QuandoUsuarioNaoExiste_DeveRetornarFalhaENaoBuscarOrganizacao()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        _identidadeMock
            .Setup(i => i.ObterPorIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainUsuarioDto?)null);

        var query = new ObterUsuarioQuery(usuarioId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _organizacaoRepositoryMock.Verify(
            r => r.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoUsuarioExisteEOrganizacaoEncontrada_DeveRetornarDtoComNomeOrganizacao()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var usuarioDomain = new DomainUsuarioDto(
            Id: usuarioId,
            TenantId: tenantId,
            Nome: "Vinícius Silva",
            Email: "vinicius@ecotrack.com.br",
            Perfil: nameof(PerfilUsuario.Administrador));
        var organizacao = new Organizacao(tenantId, "EcoTrack Ambiental", DateTime.UtcNow);

        _identidadeMock
            .Setup(i => i.ObterPorIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarioDomain);

        _organizacaoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizacao);

        var query = new ObterUsuarioQuery(usuarioId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess, string.Join(" | ", resultado.Errors.Select(e => e.Message)));
        Assert.AreEqual(usuarioId, resultado.Value.Id);
        Assert.AreEqual(tenantId, resultado.Value.TenantId);
        Assert.AreEqual("Vinícius Silva", resultado.Value.Nome);
        Assert.AreEqual("vinicius@ecotrack.com.br", resultado.Value.Email);
        Assert.AreEqual(nameof(PerfilUsuario.Administrador), resultado.Value.Perfil);
        Assert.AreEqual("EcoTrack Ambiental", resultado.Value.NomeOrganizacao);
    }

    [TestMethod]
    public async Task Handle_QuandoUsuarioExisteESemOrganizacao_DeveRetornarDtoComOrganizacaoNula()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var usuarioDomain = new DomainUsuarioDto(
            Id: usuarioId,
            TenantId: tenantId,
            Nome: "Ana Carolina",
            Email: "ana@ecotrack.com.br",
            Perfil: nameof(PerfilUsuario.Analista));

        _identidadeMock
            .Setup(i => i.ObterPorIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarioDomain);

        _organizacaoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organizacao?)null);

        var query = new ObterUsuarioQuery(usuarioId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess, string.Join(" | ", resultado.Errors.Select(e => e.Message)));
        Assert.AreEqual(usuarioId, resultado.Value.Id);
        Assert.IsNull(resultado.Value.NomeOrganizacao);
    }
}