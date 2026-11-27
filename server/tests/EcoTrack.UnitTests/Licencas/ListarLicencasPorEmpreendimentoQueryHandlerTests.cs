using EcoTrack.Application.Licencas;
using EcoTrack.Domain.Licencas;
using Moq;

namespace EcoTrack.UnitTests.Licencas;

[TestClass]
public sealed class ListarLicencasPorEmpreendimentoQueryHandlerTests
{
    private Mock<ILicencaRepository> _licencaRepositoryMock = null!;
    private ListarLicencasPorEmpreendimentoQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _licencaRepositoryMock = new Mock<ILicencaRepository>();
        _handler = new ListarLicencasPorEmpreendimentoQueryHandler(_licencaRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_QuandoExistemLicencasParaOEmpreendimento_DeveRetornarSucessoComDtosMapeados()
    {
        // Arrange
        var empreendimentoId = Guid.NewGuid();
        var licencaId = Guid.NewGuid();
        var emissao = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var validade = emissao.AddYears(2);
        var criadoEm = new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc);
        var atualizadoEm = new DateTime(2026, 4, 5, 11, 15, 0, DateTimeKind.Utc);

        var licencas = new List<Licenca>
        {
            new()
            {
                Id = licencaId,
                EmpreendimentoId = empreendimentoId,
                NumeroProcesso = "PROC-2026-200",
                NumeroLicenca = "LAI-2026-012",
                OrgaoEmissor = "IMA",
                Tipo = TipoLicenca.LAI,
                DataEmissao = emissao,
                DataValidade = validade,
                DiasAntecedenciaRenovacao = Licenca.DiasAntecedenciaRenovacaoPadrao,
                Status = StatusLicenca.Vigente,
                CreatedAtUtc = criadoEm,
                UpdatedAtUtc = atualizadoEm
            }
        };

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorEmpreendimentoIdAsync(empreendimentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licencas);

        var query = new ListarLicencasPorEmpreendimentoQuery(empreendimentoId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.HasCount(1, resultado.Value);
        Assert.AreEqual(licencaId, resultado.Value[0].Id);
        Assert.AreEqual(empreendimentoId, resultado.Value[0].EmpreendimentoId);
        Assert.AreEqual("PROC-2026-200", resultado.Value[0].NumeroProcesso);
        Assert.AreEqual("LAI-2026-012", resultado.Value[0].NumeroLicenca);
        Assert.AreEqual("IMA", resultado.Value[0].OrgaoEmissor);
        Assert.AreEqual(TipoLicenca.LAI, resultado.Value[0].Tipo);
        Assert.AreEqual(emissao, resultado.Value[0].DataEmissao);
        Assert.AreEqual(validade, resultado.Value[0].DataValidade);
        Assert.AreEqual(Licenca.DiasAntecedenciaRenovacaoPadrao, resultado.Value[0].DiasAntecedenciaRenovacao);
        Assert.AreEqual(validade.AddDays(-Licenca.DiasAntecedenciaRenovacaoPadrao), resultado.Value[0].DataLimiteProtocoloRenovacao);
        Assert.AreEqual(StatusLicenca.Vigente, resultado.Value[0].Status);
        Assert.AreEqual(criadoEm, resultado.Value[0].CreatedAtUtc);
        Assert.AreEqual(atualizadoEm, resultado.Value[0].UpdatedAtUtc);
        _licencaRepositoryMock.Verify(
            r => r.SelecionarPorEmpreendimentoIdAsync(empreendimentoId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoNaoHaLicencasParaOEmpreendimento_DeveRetornarSucessoComListaVazia()
    {
        // Arrange
        var empreendimentoId = Guid.NewGuid();

        _licencaRepositoryMock
            .Setup(r => r.SelecionarPorEmpreendimentoIdAsync(empreendimentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new ListarLicencasPorEmpreendimentoQuery(empreendimentoId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsEmpty(resultado.Value);
        _licencaRepositoryMock.Verify(
            r => r.SelecionarPorEmpreendimentoIdAsync(empreendimentoId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}