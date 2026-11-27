using EcoTrack.Application.Condicionantes;
using EcoTrack.Domain.Condicionantes;
using Moq;

namespace EcoTrack.UnitTests.Condicionantes;

[TestClass]
public sealed class ObterCondicionantePorIdQueryHandlerTests
{
    private Mock<ICondicionanteRepository> _condicionanteRepositoryMock = null!;
    private ObterCondicionantePorIdQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _condicionanteRepositoryMock = new Mock<ICondicionanteRepository>();
        _handler = new ObterCondicionantePorIdQueryHandler(_condicionanteRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_QuandoCondicionanteNaoExiste_DeveRetornarFalha()
    {
        // Arrange
        var id = Guid.NewGuid();
        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Condicionante?)null);

        var query = new ObterCondicionantePorIdQuery(id);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _condicionanteRepositoryMock.Verify(
            r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoCondicionanteExiste_DeveRetornarSucessoComDtoMapeado()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var licencaId = Guid.NewGuid();
        var evidenciaId = Guid.NewGuid();
        var vencimento = DateTime.UtcNow.AddDays(45);
        var criadoEm = new DateTime(2026, 2, 20, 8, 15, 0, DateTimeKind.Utc);
        var atualizadoEm = new DateTime(2026, 3, 25, 13, 40, 0, DateTimeKind.Utc);
        var dataUpload = new DateTime(2026, 3, 22, 10, 0, 0, DateTimeKind.Utc);

        var condicionante = new Condicionante
        {
            Id = id,
            TenantId = tenantId,
            LicencaId = licencaId,
            CodigoItem = "COND-04",
            Titulo = "Monitoramento de ruído ambiental",
            Descricao = "Medição nos limites do empreendimento.",
            Periodicidade = PeriodicidadeCondicionante.Semestral,
            DataVencimento = vencimento,
            ResponsavelTecnico = "Eng. Ambiental",
            Status = StatusCondicionante.Pendente,
            CreatedAtUtc = criadoEm,
            UpdatedAtUtc = atualizadoEm
        };

        condicionante.AnexarEvidencia(new EvidenciaCondicionante
        {
            Id = evidenciaId,
            CondicionanteId = id,
            TenantId = tenantId,
            NomeOriginalArquivo = "laudo-ruido.pdf",
            IdentificadorArmazenamento = "tenant/cond-04/laudo-ruido.pdf",
            ContentType = "application/pdf",
            TamanhoBytes = 310_000,
            NumeroArt = "ART-2026-0210",
            Observacao = "Período diurno e noturno",
            DataUploadUtc = dataUpload
        });

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        var query = new ObterCondicionantePorIdQuery(id);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(id, resultado.Value.Id);
        Assert.AreEqual(licencaId, resultado.Value.LicencaId);
        Assert.AreEqual("COND-04", resultado.Value.CodigoItem);
        Assert.AreEqual("Monitoramento de ruído ambiental", resultado.Value.Titulo);
        Assert.AreEqual("Medição nos limites do empreendimento.", resultado.Value.Descricao);
        Assert.AreEqual(PeriodicidadeCondicionante.Semestral, resultado.Value.Periodicidade);
        Assert.AreEqual(vencimento, resultado.Value.DataVencimento);
        Assert.AreEqual("Eng. Ambiental", resultado.Value.ResponsavelTecnico);
        Assert.AreEqual(StatusCondicionante.EmAndamento, resultado.Value.Status);
        Assert.AreEqual(1, resultado.Value.QuantidadeEvidencias);
        Assert.AreEqual(criadoEm, resultado.Value.CreatedAtUtc);
        Assert.IsTrue(resultado.Value.UpdatedAtUtc >= atualizadoEm);
        Assert.IsTrue((DateTime.UtcNow - resultado.Value.UpdatedAtUtc).Duration() < TimeSpan.FromSeconds(5));
        Assert.HasCount(1, resultado.Value.Evidencias);
        Assert.AreEqual(evidenciaId, resultado.Value.Evidencias[0].Id);
        Assert.AreEqual("laudo-ruido.pdf", resultado.Value.Evidencias[0].NomeOriginalArquivo);
        Assert.AreEqual("ART-2026-0210", resultado.Value.Evidencias[0].NumeroArt);

        _condicionanteRepositoryMock.Verify(
            r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}