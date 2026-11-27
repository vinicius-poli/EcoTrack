using EcoTrack.Domain.Auth;

namespace EcoTrack.UnitTests.Auth;

[TestClass]
public sealed class OrganizacaoTests
{
    [TestMethod]
    public void Construtor_QuandoDadosValidos_DeveCriarENormalizarNome()
    {
        // Arrange
        var id = Guid.NewGuid();
        var criadoEm = new DateTime(2026, 11, 27, 14, 0, 0, DateTimeKind.Utc);

        // Act
        var organizacao = new Organizacao(id, "  EcoTrack Ambiental  ", criadoEm);

        // Assert
        Assert.AreEqual(id, organizacao.Id);
        Assert.AreEqual("EcoTrack Ambiental", organizacao.Nome);
        Assert.AreEqual(criadoEm, organizacao.CriadoEmUtc);
    }

    [TestMethod]
    public void Construtor_QuandoIdVazio_DeveLancarArgumentException()
    {
        // Arrange
        var criadoEm = DateTime.UtcNow;

        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            new Organizacao(Guid.Empty, "Organização Válida", criadoEm));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Construtor_QuandoNomeInvalido_DeveLancarArgumentException(string nomeInvalido)
    {
        // Arrange
        var id = Guid.NewGuid();
        var criadoEm = DateTime.UtcNow;

        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            new Organizacao(id, nomeInvalido, criadoEm));
    }

    [TestMethod]
    public void Renomear_QuandoNovoNomeValido_DeveAtualizarENormalizarNome()
    {
        // Arrange
        var organizacao = new Organizacao(Guid.NewGuid(), "Nome Inicial", DateTime.UtcNow);

        // Act
        organizacao.Renomear("  Novo Nome Organização  ");

        // Assert
        Assert.AreEqual("Novo Nome Organização", organizacao.Nome);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Renomear_QuandoNovoNomeInvalido_DeveLancarArgumentException(string novoNomeInvalido)
    {
        // Arrange
        var organizacao = new Organizacao(Guid.NewGuid(), "Nome Inicial", DateTime.UtcNow);

        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            organizacao.Renomear(novoNomeInvalido));
    }
}