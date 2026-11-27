using EcoTrack.Application.Condicionantes;
using EcoTrack.Domain.Condicionantes;
using Moq;

namespace EcoTrack.UnitTests.Condicionantes;

[TestClass]
public sealed class ListarCondicionantesPorLicencaQueryHandlerTests
{
    private Mock<ICondicionanteRepository> _condicionanteRepositoryMock = null!;
    private ListarCondicionantesPorLicencaQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _condicionanteRepositoryMock = new Mock<ICondicionanteRepository>();
        _handler = new ListarCondicionantesPorLicencaQueryHandler(_condicionanteRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_QuandoExistemCondicionantesParaALicenca_DeveRetornarSucessoComDtosMapeados()
    {
        // Arrange
        var licencaId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var condicionanteId = Guid.NewGuid();
        var evidenciaId = Guid.NewGuid();
        var vencimento = DateTime.UtcNow.AddDays(20);
        var criadoEm = new DateTime(2026, 2, 10, 9, 0, 0, DateTimeKind.Utc);
        var atualizadoEm = new DateTime(2026, 3, 15, 16, 0, 0, DateTimeKind.Utc);
        var dataUpload = new DateTime(2026, 3, 12, 10, 30, 0, DateTimeKind.Utc);

        var condicionante = new Condicionante
        {
            Id = condicionanteId,
            TenantId = tenantId,
            LicencaId = licencaId,
            CodigoItem = "COND-02",
            Titulo = "Controle de emissões atmosféricas",
            Descricao = "Monitoramento periódico de chaminés.",
            Periodicidade = PeriodicidadeCondicionante.Trimestral,
            DataVencimento = vencimento,
            ResponsavelTecnico = "Eng. Ambiental Responsável",
            Status = StatusCondicionante.Pendente,
            CreatedAtUtc = criadoEm,
            UpdatedAtUtc = atualizadoEm
        };

        condicionante.AnexarEvidencia(new EvidenciaCondicionante
        {
            Id = evidenciaId,
            CondicionanteId = condicionanteId,
            TenantId = tenantId,
            NomeOriginalArquivo = "relatorio-emissoes.pdf",
            IdentificadorArmazenamento = "tenant/cond-02/relatorio-emissoes.pdf",
            ContentType = "application/pdf",
            TamanhoBytes = 180_000,
            NumeroArt = "ART-2026-0150",
            Observacao = "Amostragem isocinética",
            DataUploadUtc = dataUpload
        });

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorLicencaIdAsync(licencaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([condicionante]);

        var query = new ListarCondicionantesPorLicencaQuery(licencaId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.HasCount(1, resultado.Value);

        var dto = resultado.Value[0];
        Assert.AreEqual(condicionanteId, dto.Id);
        Assert.AreEqual(licencaId, dto.LicencaId);
        Assert.AreEqual("COND-02", dto.CodigoItem);
        Assert.AreEqual("Controle de emissões atmosféricas", dto.Titulo);
        Assert.AreEqual("Monitoramento periódico de chaminés.", dto.Descricao);
        Assert.AreEqual(PeriodicidadeCondicionante.Trimestral, dto.Periodicidade);
        Assert.AreEqual(vencimento, dto.DataVencimento);
        Assert.AreEqual("Eng. Ambiental Responsável", dto.ResponsavelTecnico);
        Assert.AreEqual(StatusCondicionante.EmAndamento, dto.Status);
        Assert.AreEqual(1, dto.QuantidadeEvidencias);
        Assert.AreEqual(criadoEm, dto.CreatedAtUtc);
        Assert.IsTrue(dto.UpdatedAtUtc >= atualizadoEm);
        Assert.IsTrue((DateTime.UtcNow - dto.UpdatedAtUtc).Duration() < TimeSpan.FromSeconds(5));
        Assert.HasCount(1, dto.Evidencias);
        Assert.AreEqual(evidenciaId, dto.Evidencias[0].Id);

        _condicionanteRepositoryMock.Verify(
            r => r.SelecionarPorLicencaIdAsync(licencaId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoNaoHaCondicionantesParaALicenca_DeveRetornarSucessoComListaVazia()
    {
        // Arrange
        var licencaId = Guid.NewGuid();

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorLicencaIdAsync(licencaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new ListarCondicionantesPorLicencaQuery(licencaId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsEmpty(resultado.Value);
        _condicionanteRepositoryMock.Verify(
            r => r.SelecionarPorLicencaIdAsync(licencaId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}