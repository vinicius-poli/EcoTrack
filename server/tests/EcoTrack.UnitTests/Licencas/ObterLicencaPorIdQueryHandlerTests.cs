using EcoTrack.Application.Licencas;
using EcoTrack.Domain.Licencas;
using Moq;

namespace EcoTrack.UnitTests.Licencas;

[TestClass]
public sealed class ObterLicencaPorIdQueryHandlerTests
{
    private Mock<ILicencaRepository> _licencaRepositoryMock = null!;
    private ObterLicencaPorIdQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _licencaRepositoryMock = new Mock<ILicencaRepository>();
        _handler = new ObterLicencaPorIdQueryHandler(_licencaRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_QuandoLicencaNaoExiste_DeveRetornarFalha()
    {
        // Arrange
        var id = Guid.NewGuid();
        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licenca?)null);

        var query = new ObterLicencaPorIdQuery(id);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _licencaRepositoryMock.Verify(
            r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoLicencaExiste_DeveRetornarSucessoComDtoMapeado()
    {
        // Arrange
        var id = Guid.NewGuid();
        var empreendimentoId = Guid.NewGuid();
        var emissao = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc);
        var validade = emissao.AddYears(2);
        var criadoEm = new DateTime(2026, 2, 10, 9, 30, 0, DateTimeKind.Utc);
        var atualizadoEm = new DateTime(2026, 3, 12, 17, 0, 0, DateTimeKind.Utc);

        var licenca = new Licenca
        {
            Id = id,
            EmpreendimentoId = empreendimentoId,
            NumeroProcesso = "PROC-2026-500",
            NumeroLicenca = "LAO-2026-105",
            OrgaoEmissor = "IMA",
            Tipo = TipoLicenca.LAO,
            DataEmissao = emissao,
            DataValidade = validade,
            DiasAntecedenciaRenovacao = Licenca.DiasAntecedenciaRenovacaoPadrao,
            Status = StatusLicenca.Vigente,
            CreatedAtUtc = criadoEm,
            UpdatedAtUtc = atualizadoEm
        };

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenca);

        var query = new ObterLicencaPorIdQuery(id);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(id, resultado.Value.Id);
        Assert.AreEqual(empreendimentoId, resultado.Value.EmpreendimentoId);
        Assert.AreEqual("PROC-2026-500", resultado.Value.NumeroProcesso);
        Assert.AreEqual("LAO-2026-105", resultado.Value.NumeroLicenca);
        Assert.AreEqual("IMA", resultado.Value.OrgaoEmissor);
        Assert.AreEqual(TipoLicenca.LAO, resultado.Value.Tipo);
        Assert.AreEqual(emissao, resultado.Value.DataEmissao);
        Assert.AreEqual(validade, resultado.Value.DataValidade);
        Assert.AreEqual(Licenca.DiasAntecedenciaRenovacaoPadrao, resultado.Value.DiasAntecedenciaRenovacao);
        Assert.AreEqual(validade.AddDays(-Licenca.DiasAntecedenciaRenovacaoPadrao), resultado.Value.DataLimiteProtocoloRenovacao);
        Assert.AreEqual(StatusLicenca.Vigente, resultado.Value.Status);
        Assert.AreEqual(criadoEm, resultado.Value.CreatedAtUtc);
        Assert.AreEqual(atualizadoEm, resultado.Value.UpdatedAtUtc);
        _licencaRepositoryMock.Verify(
            r => r.SelecionarPorIdAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}