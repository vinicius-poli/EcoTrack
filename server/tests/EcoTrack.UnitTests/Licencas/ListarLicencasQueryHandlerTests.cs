using EcoTrack.Application.Licencas;
using EcoTrack.Domain.Licencas;
using Moq;

namespace EcoTrack.UnitTests.Licencas;

[TestClass]
public sealed class ListarLicencasQueryHandlerTests
{
    private Mock<ILicencaRepository> _licencaRepositoryMock = null!;
    private ListarLicencasQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _licencaRepositoryMock = new Mock<ILicencaRepository>();
        _handler = new ListarLicencasQueryHandler(_licencaRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_QuandoExistemLicencas_DeveRetornarSucessoComDtosMapeados()
    {
        // Arrange
        var licencaId = Guid.NewGuid();
        var empreendimentoId = Guid.NewGuid();
        var emissao = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var validade = emissao.AddYears(2);
        var criadoEm = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var atualizadoEm = new DateTime(2026, 2, 10, 14, 20, 0, DateTimeKind.Utc);

        var licencas = new List<Licenca>
        {
            new()
            {
                Id = licencaId,
                EmpreendimentoId = empreendimentoId,
                NumeroProcesso = "PROC-2026-101",
                NumeroLicenca = "LAO-2026-055",
                OrgaoEmissor = "IMA",
                Tipo = TipoLicenca.LAO,
                DataEmissao = emissao,
                DataValidade = validade,
                DiasAntecedenciaRenovacao = Licenca.DiasAntecedenciaRenovacaoPadrao,
                Status = StatusLicenca.Vigente,
                NumeroProtocoloRenovacao = null,
                DataProtocoloRenovacao = null,
                CreatedAtUtc = criadoEm,
                UpdatedAtUtc = atualizadoEm
            }
        };

        _licencaRepositoryMock
            .Setup(r => r.SelecionarTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(licencas);

        var query = new ListarLicencasQuery();

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.HasCount(1, resultado.Value);
        Assert.AreEqual(licencaId, resultado.Value[0].Id);
        Assert.AreEqual(empreendimentoId, resultado.Value[0].EmpreendimentoId);
        Assert.AreEqual("PROC-2026-101", resultado.Value[0].NumeroProcesso);
        Assert.AreEqual("LAO-2026-055", resultado.Value[0].NumeroLicenca);
        Assert.AreEqual("IMA", resultado.Value[0].OrgaoEmissor);
        Assert.AreEqual(TipoLicenca.LAO, resultado.Value[0].Tipo);
        Assert.AreEqual(emissao, resultado.Value[0].DataEmissao);
        Assert.AreEqual(validade, resultado.Value[0].DataValidade);
        Assert.AreEqual(Licenca.DiasAntecedenciaRenovacaoPadrao, resultado.Value[0].DiasAntecedenciaRenovacao);
        Assert.AreEqual(validade.AddDays(-Licenca.DiasAntecedenciaRenovacaoPadrao), resultado.Value[0].DataLimiteProtocoloRenovacao);
        Assert.AreEqual(StatusLicenca.Vigente, resultado.Value[0].Status);
        Assert.AreEqual(criadoEm, resultado.Value[0].CreatedAtUtc);
        Assert.AreEqual(atualizadoEm, resultado.Value[0].UpdatedAtUtc);
        _licencaRepositoryMock.Verify(
            r => r.SelecionarTodosAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoNaoHaLicencas_DeveRetornarSucessoComListaVazia()
    {
        // Arrange
        _licencaRepositoryMock
            .Setup(r => r.SelecionarTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new ListarLicencasQuery();

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsEmpty(resultado.Value);
        _licencaRepositoryMock.Verify(
            r => r.SelecionarTodosAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}