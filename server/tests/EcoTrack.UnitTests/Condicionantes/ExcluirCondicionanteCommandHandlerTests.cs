using EcoTrack.Application.Condicionantes;
using EcoTrack.Domain.Condicionantes;
using Moq;

namespace EcoTrack.UnitTests.Condicionantes;

[TestClass]
public sealed class ExcluirCondicionanteCommandHandlerTests
{
    private Mock<ICondicionanteRepository> _condicionanteRepositoryMock = null!;
    private Mock<IEvidenciaStorageService> _evidenciaStorageServiceMock = null!;
    private ExcluirCondicionanteCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _condicionanteRepositoryMock = new Mock<ICondicionanteRepository>();
        _evidenciaStorageServiceMock = new Mock<IEvidenciaStorageService>();
        _handler = new ExcluirCondicionanteCommandHandler(
            _condicionanteRepositoryMock.Object,
            _evidenciaStorageServiceMock.Object);
    }

    private static Condicionante CriarCondicionante(
        Guid id,
        Guid tenantId,
        int quantidadeEvidencias = 0)
    {
        var condicionante = new Condicionante
        {
            Id = id,
            TenantId = tenantId,
            LicencaId = Guid.NewGuid(),
            CodigoItem = "COND-06",
            Titulo = "Monitoramento de efluentes atmosféricos",
            Descricao = "Coleta e análise periódica.",
            Periodicidade = PeriodicidadeCondicionante.Semestral,
            DataVencimento = new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc),
            Status = StatusCondicionante.Pendente
        };

        for (var i = 0; i < quantidadeEvidencias; i++)
        {
            condicionante.AnexarEvidencia(new EvidenciaCondicionante
            {
                Id = Guid.NewGuid(),
                NomeOriginalArquivo = $"evidencia-{i + 1}.pdf",
                ContentType = "application/pdf",
                TamanhoBytes = 120_000,
                IdentificadorArmazenamento = $"storage-key-{i + 1}"
            });
        }

        return condicionante;
    }

    [TestMethod]
    public async Task Handle_QuandoCondicionanteNaoExiste_DeveRetornarFalhaENaoAcessarStorageNemExcluir()
    {
        // Arrange
        var id = Guid.NewGuid();
        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Condicionante?)null);

        var command = new ExcluirCondicionanteCommand(id);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _evidenciaStorageServiceMock.Verify(
            s => s.RemoverAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _condicionanteRepositoryMock.Verify(
            r => r.ExcluirAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoExcluirAsyncRetornaFalse_DeveRetornarFalha()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var condicionante = CriarCondicionante(id, tenantId, quantidadeEvidencias: 1);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        _evidenciaStorageServiceMock
            .Setup(s => s.RemoverAsync("storage-key-1", tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _condicionanteRepositoryMock
            .Setup(r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new ExcluirCondicionanteCommand(id);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _evidenciaStorageServiceMock.Verify(
            s => s.RemoverAsync("storage-key-1", tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
        _condicionanteRepositoryMock.Verify(
            r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoSemEvidenciasESucesso_DeveExcluirSemMensagemDeEvidencia()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var condicionante = CriarCondicionante(id, tenantId, quantidadeEvidencias: 0);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        _condicionanteRepositoryMock
            .Setup(r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new ExcluirCondicionanteCommand(id);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.HasCount(0, resultado.Successes);
        _evidenciaStorageServiceMock.Verify(
            s => s.RemoverAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _condicionanteRepositoryMock.Verify(
            r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoComMultiplasEvidenciasESucesso_DeveRemoverArquivosEIncluirAlertaNoSucesso()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var condicionante = CriarCondicionante(id, tenantId, quantidadeEvidencias: 2);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        _evidenciaStorageServiceMock
            .Setup(s => s.RemoverAsync(It.IsAny<string>(), tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _condicionanteRepositoryMock
            .Setup(r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new ExcluirCondicionanteCommand(id);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.HasCount(1, resultado.Successes);
        Assert.Contains("2 evidências foram removidas", resultado.Successes[0].Message);
        _evidenciaStorageServiceMock.Verify(
            s => s.RemoverAsync("storage-key-1", tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
        _evidenciaStorageServiceMock.Verify(
            s => s.RemoverAsync("storage-key-2", tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
        _condicionanteRepositoryMock.Verify(
            r => r.ExcluirAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}