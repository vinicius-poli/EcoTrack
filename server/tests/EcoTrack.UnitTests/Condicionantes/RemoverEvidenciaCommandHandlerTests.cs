using EcoTrack.Application.Condicionantes;
using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Condicionantes;
using Moq;

namespace EcoTrack.UnitTests.Condicionantes;

[TestClass]
public sealed class RemoverEvidenciaCommandHandlerTests
{
    private Mock<ICondicionanteRepository> _condicionanteRepositoryMock = null!;
    private Mock<IEvidenciaStorageService> _storageServiceMock = null!;
    private Mock<IUserContext> _userContextMock = null!;
    private RemoverEvidenciaCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _condicionanteRepositoryMock = new Mock<ICondicionanteRepository>();
        _storageServiceMock = new Mock<IEvidenciaStorageService>();
        _userContextMock = new Mock<IUserContext>();
        _handler = new RemoverEvidenciaCommandHandler(
            _condicionanteRepositoryMock.Object,
            _storageServiceMock.Object,
            _userContextMock.Object);
    }

    private static Condicionante CriarCondicionanteComEvidencia(
        Guid condicionanteId,
        Guid tenantId,
        Guid evidenciaId,
        string storageKey = "tenant/cond/evidencia-1.pdf",
        StatusCondicionante statusInicial = StatusCondicionante.Pendente)
    {
        var condicionante = new Condicionante
        {
            Id = condicionanteId,
            TenantId = tenantId,
            LicencaId = Guid.NewGuid(),
            CodigoItem = "COND-07",
            Titulo = "Controle de efluentes líquidos",
            Descricao = "Amostragem periódica na saída da ETE.",
            Periodicidade = PeriodicidadeCondicionante.Trimestral,
            DataVencimento = new DateTime(2026, 12, 30, 0, 0, 0, DateTimeKind.Utc),
            Status = statusInicial
        };

        condicionante.AnexarEvidencia(new EvidenciaCondicionante
        {
            Id = evidenciaId,
            NomeOriginalArquivo = "laudo-ete.pdf",
            ContentType = "application/pdf",
            TamanhoBytes = 320_000,
            IdentificadorArmazenamento = storageKey
        });

        condicionante.Status = statusInicial;
        return condicionante;
    }

    [TestMethod]
    public async Task Handle_QuandoTenantIdNulo_DeveRetornarFalhaENaoAcessarDependencias()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns((Guid?)null);
        var command = new RemoverEvidenciaCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _condicionanteRepositoryMock.Verify(
            r => r.SelecionarPorIdComEvidenciasAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _condicionanteRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Condicionante>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _storageServiceMock.Verify(
            s => s.RemoverAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoCondicionanteNaoExiste_DeveRetornarFalhaENaoEditarNemRemoverArquivo()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.NewGuid());
        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(condicionanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Condicionante?)null);

        var command = new RemoverEvidenciaCommand(condicionanteId, Guid.NewGuid());

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _condicionanteRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Condicionante>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _storageServiceMock.Verify(
            s => s.RemoverAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoEvidenciaNaoExisteNaCondicionante_DeveRetornarFalhaENaoEditarNemRemoverArquivo()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var evidenciaExistenteId = Guid.NewGuid();
        var evidenciaInexistenteId = Guid.NewGuid();

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);

        var condicionante = CriarCondicionanteComEvidencia(
            condicionanteId,
            tenantId,
            evidenciaExistenteId);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(condicionanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        var command = new RemoverEvidenciaCommand(condicionanteId, evidenciaInexistenteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _condicionanteRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Condicionante>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _storageServiceMock.Verify(
            s => s.RemoverAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoEditarAsyncRetornaFalse_DeveRetornarFalhaENaoRemoverArquivoDoStorage()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var evidenciaId = Guid.NewGuid();
        const string storageKey = "tenant/cond/laudo-ete.pdf";

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);

        var condicionante = CriarCondicionanteComEvidencia(
            condicionanteId,
            tenantId,
            evidenciaId,
            storageKey,
            StatusCondicionante.EmAndamento);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(condicionanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        _condicionanteRepositoryMock
            .Setup(r => r.EditarAsync(condicionante, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new RemoverEvidenciaCommand(condicionanteId, evidenciaId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _storageServiceMock.Verify(
            s => s.RemoverAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoRemocaoValidaEmAndamento_DevePersistirRemoverArquivoERetornarSucessoSimples()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var evidenciaId = Guid.NewGuid();
        const string storageKey = "tenant/cond/laudo-ete.pdf";

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);

        var condicionante = CriarCondicionanteComEvidencia(
            condicionanteId,
            tenantId,
            evidenciaId,
            storageKey,
            StatusCondicionante.EmAndamento);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(condicionanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        _condicionanteRepositoryMock
            .Setup(r => r.EditarAsync(condicionante, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _storageServiceMock
            .Setup(s => s.RemoverAsync(storageKey, tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new RemoverEvidenciaCommand(condicionanteId, evidenciaId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.HasCount(0, resultado.Successes);
        Assert.IsEmpty(condicionante.Evidencias);
        _condicionanteRepositoryMock.Verify(
            r => r.EditarAsync(condicionante, It.IsAny<CancellationToken>()),
            Times.Once);
        _storageServiceMock.Verify(
            s => s.RemoverAsync(storageKey, tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoConcluidaFicaSemEvidencias_DeveReverterStatusParaEmAndamentoEIncluirAviso()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var evidenciaId = Guid.NewGuid();
        const string storageKey = "tenant/cond/laudo-final.pdf";

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);

        var condicionante = CriarCondicionanteComEvidencia(
            condicionanteId,
            tenantId,
            evidenciaId,
            storageKey,
            StatusCondicionante.Concluida);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(condicionanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        _condicionanteRepositoryMock
            .Setup(r => r.EditarAsync(condicionante, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _storageServiceMock
            .Setup(s => s.RemoverAsync(storageKey, tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new RemoverEvidenciaCommand(condicionanteId, evidenciaId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(StatusCondicionante.EmAndamento, condicionante.Status);
        Assert.HasCount(1, resultado.Successes);
        Assert.Contains("revertido de Concluída para Em Andamento", resultado.Successes[0].Message);
        _storageServiceMock.Verify(
            s => s.RemoverAsync(storageKey, tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}