using System.Text;
using EcoTrack.Application.Condicionantes;
using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Condicionantes;
using Moq;

namespace EcoTrack.UnitTests.Condicionantes;

[TestClass]
public sealed class BaixarEvidenciaQueryHandlerTests
{
    private Mock<ICondicionanteRepository> _condicionanteRepositoryMock = null!;
    private Mock<IEvidenciaStorageService> _storageServiceMock = null!;
    private Mock<IUserContext> _userContextMock = null!;
    private BaixarEvidenciaQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _condicionanteRepositoryMock = new Mock<ICondicionanteRepository>();
        _storageServiceMock = new Mock<IEvidenciaStorageService>();
        _userContextMock = new Mock<IUserContext>();
        _handler = new BaixarEvidenciaQueryHandler(
            _condicionanteRepositoryMock.Object,
            _storageServiceMock.Object,
            _userContextMock.Object);
    }

    private static Condicionante CriarCondicionanteComEvidencia(
        Guid condicionanteId,
        Guid tenantId,
        Guid evidenciaId,
        string storageKey = "tenant/cond/laudo.pdf",
        string nomeArquivo = "laudo.pdf",
        string contentType = "application/pdf",
        long tamanhoBytes = 2048)
    {
        var condicionante = new Condicionante
        {
            Id = condicionanteId,
            TenantId = tenantId,
            LicencaId = Guid.NewGuid(),
            CodigoItem = "COND-09",
            Titulo = "Monitoramento ambiental",
            Descricao = "Relatório técnico periódico.",
            Periodicidade = PeriodicidadeCondicionante.Semestral,
            DataVencimento = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Status = StatusCondicionante.Pendente
        };

        condicionante.AnexarEvidencia(new EvidenciaCondicionante
        {
            Id = evidenciaId,
            NomeOriginalArquivo = nomeArquivo,
            IdentificadorArmazenamento = storageKey,
            ContentType = contentType,
            TamanhoBytes = tamanhoBytes
        });

        return condicionante;
    }

    [TestMethod]
    public async Task Handle_QuandoTenantIdNulo_DeveRetornarFalhaENaoAcessarDependencias()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns((Guid?)null);
        var query = new BaixarEvidenciaQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _condicionanteRepositoryMock.Verify(
            r => r.SelecionarPorIdComEvidenciasAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _storageServiceMock.Verify(
            s => s.ObterStreamAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoIdsVazios_DeveRetornarFalhaENaoAcessarDependencias()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.NewGuid());
        var queryCondicionanteVazia = new BaixarEvidenciaQuery(Guid.Empty, Guid.NewGuid());
        var queryEvidenciaVazia = new BaixarEvidenciaQuery(Guid.NewGuid(), Guid.Empty);

        // Act
        var resultadoCondicionante = await _handler.Handle(queryCondicionanteVazia, CancellationToken.None);
        var resultadoEvidencia = await _handler.Handle(queryEvidenciaVazia, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultadoCondicionante.IsFailed);
        Assert.IsTrue(resultadoEvidencia.IsFailed);
        _condicionanteRepositoryMock.Verify(
            r => r.SelecionarPorIdComEvidenciasAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _storageServiceMock.Verify(
            s => s.ObterStreamAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoCondicionanteNaoExiste_DeveRetornarFalhaENaoAcessarStorage()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.NewGuid());
        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(condicionanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Condicionante?)null);

        var query = new BaixarEvidenciaQuery(condicionanteId, Guid.NewGuid());

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _storageServiceMock.Verify(
            s => s.ObterStreamAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoEvidenciaNaoExisteNaCondicionante_DeveRetornarFalhaENaoAcessarStorage()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var evidenciaExistenteId = Guid.NewGuid();
        var evidenciaBuscadaId = Guid.NewGuid();

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);

        var condicionante = CriarCondicionanteComEvidencia(
            condicionanteId,
            tenantId,
            evidenciaExistenteId);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(condicionanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        var query = new BaixarEvidenciaQuery(condicionanteId, evidenciaBuscadaId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _storageServiceMock.Verify(
            s => s.ObterStreamAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoStreamNaoEncontradoNoStorage_DeveRetornarFalha()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var evidenciaId = Guid.NewGuid();
        const string storageKey = "tenant/cond/ausente.pdf";

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);

        var condicionante = CriarCondicionanteComEvidencia(
            condicionanteId,
            tenantId,
            evidenciaId,
            storageKey);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(condicionanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        _storageServiceMock
            .Setup(s => s.ObterStreamAsync(storageKey, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        var query = new BaixarEvidenciaQuery(condicionanteId, evidenciaId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _storageServiceMock.Verify(
            s => s.ObterStreamAsync(storageKey, tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoEvidenciaEArquivoExistem_DeveRetornarSucessoComStreamEMetadados()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var evidenciaId = Guid.NewGuid();
        const string storageKey = "tenant/cond/laudo-valido.pdf";
        const string nomeArquivo = "laudo-valido.pdf";
        const string contentType = "application/pdf";
        const long tamanhoBytes = 4096;

        await using var streamEsperado = new MemoryStream(Encoding.UTF8.GetBytes("pdf-bytes"));

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);

        var condicionante = CriarCondicionanteComEvidencia(
            condicionanteId,
            tenantId,
            evidenciaId,
            storageKey,
            nomeArquivo,
            contentType,
            tamanhoBytes);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(condicionanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        _storageServiceMock
            .Setup(s => s.ObterStreamAsync(storageKey, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streamEsperado);

        var query = new BaixarEvidenciaQuery(condicionanteId, evidenciaId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreSame(streamEsperado, resultado.Value.Conteudo);
        Assert.AreEqual(nomeArquivo, resultado.Value.NomeOriginalArquivo);
        Assert.AreEqual(contentType, resultado.Value.ContentType);
        Assert.AreEqual(tamanhoBytes, resultado.Value.TamanhoBytes);
        _storageServiceMock.Verify(
            s => s.ObterStreamAsync(storageKey, tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}