using EcoTrack.Application.Empreendimentos;
using EcoTrack.Domain.Empreendimentos;
using Moq;

namespace EcoTrack.UnitTests.Empreendimentos;

[TestClass]
public sealed class ListarEmpreendimentosQueryHandlerTests
{
    private Mock<IEmpreendimentoRepository> _empreendimentoRepositoryMock = null!;
    private ListarEmpreendimentosQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _empreendimentoRepositoryMock = new Mock<IEmpreendimentoRepository>();
        _handler = new ListarEmpreendimentosQueryHandler(_empreendimentoRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_QuandoExistemEmpreendimentos_DeveRetornarSucessoComDtosMapeados()
    {
        // Arrange
        var empreendimentoId = Guid.NewGuid();
        var criadoEm = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);
        var atualizadoEm = new DateTime(2026, 6, 15, 15, 30, 0, DateTimeKind.Utc);

        var empreendimentos = new List<Empreendimento>
        {
            new()
            {
                Id = empreendimentoId,
                RazaoSocial = "Papel e Celulose Serra Ltda",
                NomeFantasia = "Unidade Lages",
                Cnpj = "45678912378952",
                Municipio = "Lages",
                Uf = "SC",
                Porte = PorteEmpreendimento.Medio,
                StatusLicenciamento = StatusLicenciamento.Licenciado,
                CreatedAtUtc = criadoEm,
                UpdatedAtUtc = atualizadoEm
            }
        };

        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(empreendimentos);

        var query = new ListarEmpreendimentosQuery();

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.HasCount(1, resultado.Value);
        Assert.AreEqual(empreendimentoId, resultado.Value[0].Id);
        Assert.AreEqual("Papel e Celulose Serra Ltda", resultado.Value[0].RazaoSocial);
        Assert.AreEqual("Unidade Lages", resultado.Value[0].NomeFantasia);
        Assert.AreEqual("45678912378952", resultado.Value[0].Cnpj);
        Assert.AreEqual("Lages", resultado.Value[0].Municipio);
        Assert.AreEqual("SC", resultado.Value[0].Uf);
        Assert.AreEqual("Medio", resultado.Value[0].Porte);
        Assert.AreEqual("Licenciado", resultado.Value[0].StatusLicenciamento);
        Assert.AreEqual(criadoEm, resultado.Value[0].CreatedAtUtc);
        Assert.AreEqual(atualizadoEm, resultado.Value[0].UpdatedAtUtc);
        _empreendimentoRepositoryMock.Verify(
            r => r.SelecionarTodosAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoNaoHaEmpreendimentos_DeveRetornarSucessoComListaVazia()
    {
        // Arrange
        _empreendimentoRepositoryMock
            .Setup(r => r.SelecionarTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new ListarEmpreendimentosQuery();

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsEmpty(resultado.Value);
        _empreendimentoRepositoryMock.Verify(
            r => r.SelecionarTodosAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}