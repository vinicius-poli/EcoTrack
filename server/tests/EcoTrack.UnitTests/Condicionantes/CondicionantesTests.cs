using EcoTrack.Domain.Condicionantes;

namespace EcoTrack.UnitTests.Condicionantes;

[TestClass]
public sealed class CondicionanteTests
{
    private static Condicionante CriarCondicionanteValida(
        DateTime? dataVencimento = null,
        StatusCondicionante status = StatusCondicionante.Pendente) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        LicencaId = Guid.NewGuid(),
        CodigoItem = "COND-01",
        Titulo = "Monitoramento de efluentes",
        Descricao = "Realizar coleta e análise trimestral na saída da ETE.",
        Periodicidade = PeriodicidadeCondicionante.Trimestral,
        DataVencimento = dataVencimento ?? new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
        ResponsavelTecnico = "Eng. Ambiental Teste",
        Status = status
    };

    private static EvidenciaCondicionante CriarEvidenciaValida(
        string nomeArquivo = "laudo-efluentes.pdf",
        long tamanhoBytes = 1024 * 512) => new()
    {
        Id = Guid.NewGuid(),
        EnviadoPorUserId = Guid.NewGuid(),
        NomeOriginalArquivo = nomeArquivo,
        IdentificadorArmazenamento = "evidencias/2026/laudo-efluentes.pdf",
        ContentType = "application/pdf",
        TamanhoBytes = tamanhoBytes,
        NumeroArt = "art-98765",
        Observacao = "Amostra coletada na caixa de inspeção."
    };

    [TestMethod]
    public void Validar_QuandoDadosValidos_DeveRetornarListaVaziaENormalizarCampos()
    {
        // Arrange
        var condicionante = new Condicionante
        {
            LicencaId = Guid.NewGuid(),
            CodigoItem = "  COND-02  ",
            Titulo = "  Relatório de resíduos  ",
            Descricao = "  Enviar MTR consolidado ao órgão ambiental.  ",
            Periodicidade = PeriodicidadeCondicionante.Mensal,
            DataVencimento = new DateTime(2026, 12, 20, 0, 0, 0, DateTimeKind.Utc),
            ResponsavelTecnico = "   ",
            Status = StatusCondicionante.Pendente
        };

        // Act
        var erros = condicionante.Validar();

        // Assert
        Assert.IsEmpty(erros);
        Assert.AreEqual("COND-02", condicionante.CodigoItem);
        Assert.AreEqual("Relatório de resíduos", condicionante.Titulo);
        Assert.AreEqual("Enviar MTR consolidado ao órgão ambiental.", condicionante.Descricao);
        Assert.IsNull(condicionante.ResponsavelTecnico);
    }

    [TestMethod]
    public void Validar_QuandoCamposObrigatoriosAusentes_DeveRetornarErrosEsperados()
    {
        // Arrange
        var condicionante = new Condicionante
        {
            LicencaId = Guid.Empty,
            CodigoItem = " ",
            Titulo = "",
            Descricao = "   ",
            Periodicidade = PeriodicidadeCondicionante.Unica,
            DataVencimento = new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc),
            Status = StatusCondicionante.Pendente
        };

        // Act
        var erros = condicionante.Validar();

        // Assert
        Assert.Contains("A licença é obrigatória.", erros);
        Assert.Contains("O código do item é obrigatório.", erros);
        Assert.Contains("O título da condicionante é obrigatório.", erros);
        Assert.Contains("A descrição detalhada é obrigatória.", erros);
    }

    [TestMethod]
    public void Validar_QuandoStatusConcluidaSemEvidencias_DeveRetornarErro()
    {
        // Arrange
        var condicionante = CriarCondicionanteValida(status: StatusCondicionante.Concluida);

        // Act
        var erros = condicionante.Validar();

        // Assert
        Assert.Contains(
            "Não é possível concluir uma condicionante sem ao menos uma evidência anexada.",
            erros);
    }

    [TestMethod]
    public void AnexarEvidencia_QuandoPendenteEValida_DeveAdicionarEvidenciaETransicionarParaEmAndamento()
    {
        // Arrange
        var condicionante = CriarCondicionanteValida(status: StatusCondicionante.Pendente);
        var evidencia = CriarEvidenciaValida();

        // Act
        var erros = condicionante.AnexarEvidencia(evidencia);

        // Assert
        Assert.IsEmpty(erros);
        Assert.HasCount(1, condicionante.Evidencias);
        Assert.AreEqual(condicionante.Id, evidencia.CondicionanteId);
        Assert.AreEqual(condicionante.TenantId, evidencia.TenantId);
        Assert.AreEqual("ART-98765", evidencia.NumeroArt);
        Assert.AreEqual(StatusCondicionante.EmAndamento, condicionante.Status);
    }

    [TestMethod]
    public void AnexarEvidencia_QuandoNulaOuInvalida_DeveRetornarErroENaoAdicionar()
    {
        // Arrange
        var condicionante = CriarCondicionanteValida();
        var evidenciaInvalida = new EvidenciaCondicionante
        {
            NomeOriginalArquivo = "script.exe",
            IdentificadorArmazenamento = "",
            ContentType = "",
            TamanhoBytes = EvidenciaCondicionante.TamanhoMaximoBytesPadrao + 1
        };

        // Act
        var errosNula = condicionante.AnexarEvidencia(null!);
        var errosInvalida = condicionante.AnexarEvidencia(evidenciaInvalida);

        // Assert
        Assert.Contains("A evidência é obrigatória.", errosNula);
        Assert.Contains(
            "Formato de arquivo não permitido. Use .pdf, .jpg, .jpeg, .png, .xlsx ou .docx.",
            errosInvalida);
        Assert.Contains("O arquivo excede o tamanho máximo permitido de 10 MB.", errosInvalida);
        Assert.IsEmpty(condicionante.Evidencias);
    }

    [TestMethod]
    public void AlterarStatus_ParaConcluidaSemEvidencias_DeveBloquearEManterStatusAtual()
    {
        // Arrange
        var condicionante = CriarCondicionanteValida(status: StatusCondicionante.EmAndamento);

        // Act
        var erros = condicionante.AlterarStatus(StatusCondicionante.Concluida);

        // Assert
        Assert.Contains(
            "Não é possível alterar o status para Concluída sem ao menos uma evidência anexada.",
            erros);
        Assert.AreEqual(StatusCondicionante.EmAndamento, condicionante.Status);
    }

    [TestMethod]
    public void AlterarStatus_ParaConcluidaComEvidencia_DevePermitir()
    {
        // Arrange
        var condicionante = CriarCondicionanteValida();
        condicionante.AnexarEvidencia(CriarEvidenciaValida());

        // Act
        var erros = condicionante.AlterarStatus(StatusCondicionante.Concluida);

        // Assert
        Assert.IsEmpty(erros);
        Assert.AreEqual(StatusCondicionante.Concluida, condicionante.Status);
    }

    [TestMethod]
    public void RemoverEvidencia_QuandoConcluidaERemoveUltimaEvidencia_DeveVoltarParaEmAndamento()
    {
        // Arrange
        var condicionante = CriarCondicionanteValida();
        var evidencia = CriarEvidenciaValida();
        condicionante.AnexarEvidencia(evidencia);
        condicionante.AlterarStatus(StatusCondicionante.Concluida);

        // Act
        var erros = condicionante.RemoverEvidencia(evidencia.Id);

        // Assert
        Assert.IsEmpty(erros);
        Assert.IsEmpty(condicionante.Evidencias);
        Assert.AreEqual(StatusCondicionante.EmAndamento, condicionante.Status);
    }

    [TestMethod]
    public void RemoverEvidencia_QuandoIdInexistente_DeveRetornarErro()
    {
        // Arrange
        var condicionante = CriarCondicionanteValida();

        // Act
        var erros = condicionante.RemoverEvidencia(Guid.NewGuid());

        // Assert
        Assert.Contains("Evidência não encontrada na condicionante.", erros);
    }

    [TestMethod]
    public void CalcularNivelRisco_QuandoConcluida_DeveRetornarConcluidoMesmoVencida()
    {
        // Arrange
        var vencimento = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var referencia = new DateTime(2026, 11, 27, 0, 0, 0, DateTimeKind.Utc);
        var condicionante = CriarCondicionanteValida(
            dataVencimento: vencimento,
            status: StatusCondicionante.Concluida);

        // Act
        var nivel = condicionante.CalcularNivelRisco(referencia);

        // Assert
        Assert.AreEqual(NivelRiscoCondicionante.Concluido, nivel);
    }

    [TestMethod]
    [DataRow(-1, NivelRiscoCondicionante.Critico)]
    [DataRow(0, NivelRiscoCondicionante.Alerta)]
    [DataRow(30, NivelRiscoCondicionante.Alerta)]
    [DataRow(31, NivelRiscoCondicionante.Normal)]
    public void CalcularNivelRisco_DeveRespeitarPrazoEJanelaDeAlerta(
        int diasParaVencimento,
        NivelRiscoCondicionante nivelEsperado)
    {
        // Arrange
        var referencia = new DateTime(2026, 11, 27, 0, 0, 0, DateTimeKind.Utc);
        var condicionante = CriarCondicionanteValida(
            dataVencimento: referencia.AddDays(diasParaVencimento),
            status: StatusCondicionante.Pendente);

        // Act
        var nivel = condicionante.CalcularNivelRisco(referencia);

        // Assert
        Assert.AreEqual(nivelEsperado, nivel);
    }

    [TestMethod]
    [DataRow(PeriodicidadeCondicionante.Unica)]
    [DataRow(PeriodicidadeCondicionante.Mensal)]
    [DataRow(PeriodicidadeCondicionante.Trimestral)]
    [DataRow(PeriodicidadeCondicionante.Semestral)]
    [DataRow(PeriodicidadeCondicionante.Anual)]
    [DataRow(PeriodicidadeCondicionante.Continua)]
    public void Validar_QuandoPeriodicidadeValida_NaoDeveRetornarErro(
        PeriodicidadeCondicionante periodicidade)
    {
        // Arrange
        var condicionante = CriarCondicionanteValida();
        condicionante.Periodicidade = periodicidade;

        // Act
        var erros = condicionante.Validar();

        // Assert
        Assert.DoesNotContain("A periodicidade é inválida.", erros);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(7)]
    [DataRow(-1)]
    public void Validar_QuandoPeriodicidadeForaDoEnum_DeveRetornarErro(int valorPeriodicidade)
    {
        // Arrange
        var condicionante = CriarCondicionanteValida();
        condicionante.Periodicidade = (PeriodicidadeCondicionante)valorPeriodicidade;

        // Act
        var erros = condicionante.Validar();

        // Assert
        Assert.Contains("A periodicidade é inválida.", erros);
    }

    [TestMethod]
    public void Atualizar_DeveAtualizarCamposENormalizarTextos()
    {
        // Arrange
        var original = CriarCondicionanteValida();
        var novaLicencaId = Guid.NewGuid();
        var novaData = new DateTime(2027, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var atualizada = new Condicionante
        {
            LicencaId = novaLicencaId,
            CodigoItem = "  COND-10  ",
            Titulo = "  Novo título  ",
            Descricao = "  Nova descrição  ",
            Periodicidade = PeriodicidadeCondicionante.Anual,
            DataVencimento = novaData,
            ResponsavelTecnico = "  Eng. Responsável  ",
            Status = StatusCondicionante.EmAndamento
        };

        // Act
        original.Atualizar(atualizada);

        // Assert
        Assert.AreEqual(novaLicencaId, original.LicencaId);
        Assert.AreEqual("COND-10", original.CodigoItem);
        Assert.AreEqual("Novo título", original.Titulo);
        Assert.AreEqual("Nova descrição", original.Descricao);
        Assert.AreEqual(PeriodicidadeCondicionante.Anual, original.Periodicidade);
        Assert.AreEqual(novaData, original.DataVencimento);
        Assert.AreEqual("Eng. Responsável", original.ResponsavelTecnico);
        Assert.AreEqual(StatusCondicionante.EmAndamento, original.Status);
    }
}