using EcoTrack.Application.Empreendimentos;
using EcoTrack.Domain.Empreendimentos;
using Moq;

namespace EcoTrack.UnitTests.Empreendimentos;

[TestClass]
public sealed class ObterEmpreendimentoPorIdQueryHandlerTests
{
    private Mock<IEmpreendimentoRepository> _empreendimentoRepositoryMock = null!;
    private ObterEmpreendimentoPorIdQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _empreendimentoRepositoryMock = new Mock<IEmpreendimentoRepository>();
        _handler = new ObterEmpreendimentoPorIdQueryHandler(_empreendimentoRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_QuandoEmpreendimentoNaoExiste_DeveRetornarFalha()
    {
        // Arrange
        var id = Guid.NewGuid();
        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Empreendimento?)null);

        var query = new ObterEmpreendimentoPorIdQuery(id);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _empreendimentoRepositoryMock.Verify(
            r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoEmpreendimentoExiste_DeveRetornarSucessoComDtoMapeado()
    {
        // Arrange
        var id = Guid.NewGuid();
        var criadoEm = new DateTime(2026, 4, 12, 9, 0, 0, DateTimeKind.Utc);
        var atualizadoEm = new DateTime(2026, 7, 20, 16, 45, 0, DateTimeKind.Utc);

        var empreendimento = new Empreendimento
        {
            Id = id,
            RazaoSocial = "Papel e Celulose Serra Ltda",
            NomeFantasia = "Unidade Lages",
            Cnpj = "45678912378952",
            Municipio = "Lages",
            Uf = "SC",
            Porte = PorteEmpreendimento.Grande,
            StatusLicenciamento = StatusLicenciamento.Licenciado,
            CreatedAtUtc = criadoEm,
            UpdatedAtUtc = atualizadoEm
        };

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empreendimento);

        var query = new ObterEmpreendimentoPorIdQuery(id);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(id, resultado.Value.Id);
        Assert.AreEqual("Papel e Celulose Serra Ltda", resultado.Value.RazaoSocial);
        Assert.AreEqual("Unidade Lages", resultado.Value.NomeFantasia);
        Assert.AreEqual("45678912378952", resultado.Value.Cnpj);
        Assert.AreEqual("Lages", resultado.Value.Municipio);
        Assert.AreEqual("SC", resultado.Value.Uf);
        Assert.AreEqual("Grande", resultado.Value.Porte);
        Assert.AreEqual("Licenciado", resultado.Value.StatusLicenciamento);
        Assert.AreEqual(criadoEm, resultado.Value.CreatedAtUtc);
        Assert.AreEqual(atualizadoEm, resultado.Value.UpdatedAtUtc);
        _empreendimentoRepositoryMock.Verify(
            r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}