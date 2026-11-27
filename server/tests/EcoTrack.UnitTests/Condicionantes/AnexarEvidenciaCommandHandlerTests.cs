using System.Text;
using EcoTrack.Application.Condicionantes;
using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Condicionantes;
using Moq;

namespace EcoTrack.UnitTests.Condicionantes;

[TestClass]
public sealed class AnexarEvidenciaCommandHandlerTests
{
    private Mock<ICondicionanteRepository> _condicionanteRepositoryMock = null!;
    private Mock<IEvidenciaStorageService> _storageServiceMock = null!;
    private Mock<IUserContext> _userContextMock = null!;
    private AnexarEvidenciaCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _condicionanteRepositoryMock = new Mock<ICondicionanteRepository>();
        _storageServiceMock = new Mock<IEvidenciaStorageService>();
        _userContextMock = new Mock<IUserContext>();
        _handler = new AnexarEvidenciaCommandHandler(
            _condicionanteRepositoryMock.Object,
            _storageServiceMock.Object,
            _userContextMock.Object);
    }

    private static string ObterExtensaoValida()
    {
        var primeira = EvidenciaCondicionante.ExtensoesPermitidas.FirstOrDefault();
        return string.IsNullOrWhiteSpace(primeira) ? "pdf" : primeira.TrimStart('.');
    }

    private static AnexarEvidenciaRequest CriarRequestValido(
        Guid condicionanteId,
        long tamanhoBytes = 1024,
        string? nomeArquivo = null,
        string contentType = "application/pdf")
    {
        var ext = ObterExtensaoValida();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("conteudo-evidencia"));

        return new AnexarEvidenciaRequest(
            CondicionanteId: condicionanteId,
            Conteudo: stream,
            NomeOriginalArquivo: nomeArquivo ?? $"  relatorio-ambiental  .{ext}",
            ContentType: $"  {contentType}  ",
            TamanhoBytes: tamanhoBytes,
            NumeroArt: "ART-2026-0300",
            Observacao: "Documento anexado nos testes"
        );
    }

    private static Condicionante CriarCondicionante(
        Guid id,
        Guid tenantId,
        StatusCondicionante status = StatusCondicionante.Pendente)
    {
        return new Condicionante
        {
            Id = id,
            TenantId = tenantId,
            LicencaId = Guid.NewGuid(),
            CodigoItem = "COND-08",
            Titulo = "Monitoramento de efluentes",
            Descricao = "Relatório periódico de conformidade.",
            Periodicidade = PeriodicidadeCondicionante.Semestral,
            DataVencimento = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Status = status
        };
    }

    [TestMethod]
    public async Task Handle_QuandoTenantIdNulo_DeveRetornarFalhaENaoAcessarDependencias()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns((Guid?)null);
        var command = new AnexarEvidenciaCommand(CriarRequestValido(Guid.NewGuid()));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _condicionanteRepositoryMock.Verify(
            r => r.SelecionarPorIdComEvidenciasAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _storageServiceMock.Verify(
            s => s.SalvarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoArquivoInvalido_DeveRetornarFalhaENaoConsultarCondicionanteNemSalvar()
    {
        // Arrange
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.NewGuid());
        var requestInvalido = CriarRequestValido(
            Guid.NewGuid(),
            tamanhoBytes: 0,
            nomeArquivo: "arquivo.exe");
        var command = new AnexarEvidenciaCommand(requestInvalido);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _condicionanteRepositoryMock.Verify(
            r => r.SelecionarPorIdComEvidenciasAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _storageServiceMock.Verify(
            s => s.SalvarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoCondicionanteNaoExiste_DeveRetornarFalhaENaoSalvarNoStorage()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        _userContextMock.SetupGet(u => u.TenantId).Returns(Guid.NewGuid());
        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(condicionanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Condicionante?)null);

        var command = new AnexarEvidenciaCommand(CriarRequestValido(condicionanteId));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _storageServiceMock.Verify(
            s => s.SalvarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoAnexarEvidenciaFalhaNoDominio_DeveDesfazerUploadENaoEditar()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        const string identificadorVazio = "";
        var condicionante = CriarCondicionante(condicionanteId, tenantId);

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);
        _userContextMock.SetupGet(u => u.UserId).Returns(Guid.NewGuid());

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(condicionanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        var request = CriarRequestValido(condicionanteId);

        _storageServiceMock
            .Setup(s => s.SalvarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(identificadorVazio);

        _storageServiceMock
            .Setup(s => s.RemoverAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new AnexarEvidenciaCommand(request);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _storageServiceMock.Verify(
            s => s.SalvarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _storageServiceMock.Verify(
            s => s.RemoverAsync(identificadorVazio, tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
        _condicionanteRepositoryMock.Verify(
            r => r.EditarAsync(It.IsAny<Condicionante>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuandoEditarAsyncRetornaFalse_DeveDesfazerUploadERetornarFalha()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        const string storageKey = "tenant/cond/relatorio.pdf";
        var condicionante = CriarCondicionante(condicionanteId, tenantId);

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);
        _userContextMock.SetupGet(u => u.UserId).Returns(Guid.NewGuid());

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(condicionanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        var request = CriarRequestValido(condicionanteId);

        _storageServiceMock
            .Setup(s => s.SalvarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageKey);

        _condicionanteRepositoryMock
            .Setup(r => r.EditarAsync(
                It.IsAny<Condicionante>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _storageServiceMock
            .Setup(s => s.RemoverAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new AnexarEvidenciaCommand(request);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        _storageServiceMock.Verify(
            s => s.SalvarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _storageServiceMock.Verify(
            s => s.RemoverAsync(storageKey, tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_QuandoDadosValidos_DeveSalvarAnexarPersistirERetornarIdDaEvidencia()
    {
        // Arrange
        var condicionanteId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string storageKey = "tenant/cond/relatorio-valido.pdf";
        var condicionante = CriarCondicionante(condicionanteId, tenantId, StatusCondicionante.Pendente);
        var request = CriarRequestValido(condicionanteId);

        _userContextMock.SetupGet(u => u.TenantId).Returns(tenantId);
        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        _condicionanteRepositoryMock
            .Setup(r => r.SelecionarPorIdComEvidenciasAsync(condicionanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condicionante);

        _storageServiceMock
            .Setup(s => s.SalvarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageKey);

        _condicionanteRepositoryMock
            .Setup(r => r.EditarAsync(
                It.IsAny<Condicionante>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new AnexarEvidenciaCommand(request);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(resultado.IsSuccess, string.Join(" | ", resultado.Errors.Select(e => e.Message)));
        Assert.AreNotEqual(Guid.Empty, resultado.Value);
        Assert.HasCount(1, condicionante.Evidencias);
        Assert.AreEqual(StatusCondicionante.EmAndamento, condicionante.Status);
        Assert.AreEqual(storageKey, condicionante.Evidencias.First().IdentificadorArmazenamento);
        _storageServiceMock.Verify(
            s => s.RemoverAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _condicionanteRepositoryMock.Verify(
            r => r.EditarAsync(condicionante, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}