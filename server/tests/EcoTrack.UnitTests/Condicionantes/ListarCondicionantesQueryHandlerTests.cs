using EcoTrack.Application.Condicionantes;
using EcoTrack.Domain.Condicionantes;
using Moq;

namespace EcoTrack.UnitTests.Condicionantes;

[TestClass]
public sealed class ListarCondicionantesQueryHandlerTests
{
    private Mock<ICondicionanteRepository> _condicionanteRepositoryMock = null!;
    private ListarCondicionantesQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _condicionanteRepositoryMock = new Mock<ICondicionanteRepository>();
        _handler = new ListarCondicionantesQueryHandler(_condicionanteRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_QuandoExistemCondicionantes_DeveRetornarSucessoComDtosMapeados()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var licencaId = Guid.NewGuid();
        var evidenciaId = Guid.NewGuid();
        var vencimento = DateTime.UtcNow.AddDays(30);
        var criadoEm = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc);
        var atualizadoEm = new DateTime(2026, 3, 10, 14, 30, 0, DateTimeKind.Utc);
        var dataUpload = new DateTime(2026, 3, 5, 11, 15, 0, DateTimeKind.Utc);

        var condicionante = new Condicionante
        {
            Id = condicionanteId,
            TenantId = tenantId,
            LicencaId = licencaId,
            CodigoItem = "COND-01",
            Titulo = "Monitoramento de efluentes",
            Descricao = "Relatório semestral de DBO e DQO.",
            Periodicidade = PeriodicidadeCondicionante.Semestral,
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
            NomeOriginalArquivo = "laudo-efluentes.pdf",
            IdentificadorArmazenamento = "tenant/cond-01/laudo-efluentes.pdf",
            ContentType = "application/pdf",
            TamanhoBytes = 245_000,
            NumeroArt = "ART-2026-0099",
            Observacao = "Coleta validada em laboratório",
            DataUploadUtc = dataUpload
        });

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([condicionante]);

        var query = new ListarCondicionantesQuery();

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.HasCount(1, resultado.Value);

        var dto = resultado.Value[0];
        Assert.AreEqual(condicionanteId, dto.Id);
        Assert.AreEqual(licencaId, dto.LicencaId);
        Assert.AreEqual("COND-01", dto.CodigoItem);
        Assert.AreEqual("Monitoramento de efluentes", dto.Titulo);
        Assert.AreEqual("Relatório semestral de DBO e DQO.", dto.Descricao);
        Assert.AreEqual(PeriodicidadeCondicionante.Semestral, dto.Periodicidade);
        Assert.AreEqual(vencimento, dto.DataVencimento);
        Assert.AreEqual("Eng. Ambiental Responsável", dto.ResponsavelTecnico);
        Assert.AreEqual(StatusCondicionante.EmAndamento, dto.Status);
        Assert.AreEqual(1, dto.QuantidadeEvidencias);
        Assert.AreEqual(criadoEm, dto.CreatedAtUtc);
        Assert.IsTrue(dto.UpdatedAtUtc >= atualizadoEm);
        Assert.IsTrue((DateTime.UtcNow - dto.UpdatedAtUtc).Duration() < TimeSpan.FromSeconds(5));
        Assert.HasCount(1, dto.Evidencias);
        Assert.AreEqual(evidenciaId, dto.Evidencias[0].Id);
        Assert.AreEqual("laudo-efluentes.pdf", dto.Evidencias[0].NomeOriginalArquivo);
        Assert.AreEqual("ART-2026-0099", dto.Evidencias[0].NumeroArt);

        _condicionanteRepositoryMock.Verify(
            r => r.SelecionarTodosAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoNaoHaCondicionantes_DeveRetornarSucessoComListaVazia()
    {
        // Arrange
        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new ListarCondicionantesQuery();

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsEmpty(resultado.Value);
        _condicionanteRepositoryMock.Verify(
            r => r.SelecionarTodosAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}