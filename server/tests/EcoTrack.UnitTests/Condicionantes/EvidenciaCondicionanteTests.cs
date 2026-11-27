using EcoTrack.Domain.Condicionantes;

namespace EcoTrack.UnitTests.Condicionantes;

[TestClass]
public sealed class EvidenciaCondicionanteTests
{
    private static EvidenciaCondicionante CriarEvidenciaValida()
    {
        return new EvidenciaCondicionante
        {
            CondicionanteId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            EnviadoPorUserId = Guid.NewGuid(),
            NomeOriginalArquivo = "laudo-efluentes.pdf",
            IdentificadorArmazenamento = "tenant-1/cond-1/laudo-efluentes.pdf",
            ContentType = "application/pdf",
            TamanhoBytes = 512_000,
            NumeroArt = "art-2026-001",
            Observacao = "Amostra coletada na saída da ETE"
        };
    }

    [TestMethod]
    public void Validar_QuandoDadosValidos_DeveRetornarSemErrosENormalizarCampos()
    {
        // Arrange
        var evidencia = new EvidenciaCondicionante
        {
            NomeOriginalArquivo = "  Relatorio-Final.PDF  ",
            IdentificadorArmazenamento = "  storage/key-01.pdf  ",
            ContentType = "  application/pdf  ",
            TamanhoBytes = EvidenciaCondicionante.TamanhoMaximoBytesPadrao,
            NumeroArt = "  art-2026-xyz  ",
            Observacao = "  Observação válida  "
        };

        // Act
        var erros = evidencia.Validar();

        // Assert
        Assert.IsEmpty(erros);
        Assert.AreEqual("Relatorio-Final.PDF", evidencia.NomeOriginalArquivo);
        Assert.AreEqual("storage/key-01.pdf", evidencia.IdentificadorArmazenamento);
        Assert.AreEqual("application/pdf", evidencia.ContentType);
        Assert.AreEqual("ART-2026-XYZ", evidencia.NumeroArt);
        Assert.AreEqual("Observação válida", evidencia.Observacao);
    }

    [TestMethod]
    public void Validar_QuandoCamposObrigatoriosFaltam_DeveRetornarErros()
    {
        // Arrange
        var evidencia = new EvidenciaCondicionante
        {
            NomeOriginalArquivo = "   ",
            IdentificadorArmazenamento = "",
            ContentType = " ",
            TamanhoBytes = 1024
        };

        // Act
        var erros = evidencia.Validar();

        // Assert
        Assert.IsNotEmpty(erros);
    }

    [TestMethod]
    public void Validar_QuandoTamanhoInvalidoOuAcimaDoLimite_DeveRetornarErro()
    {
        // Arrange
        var evidenciaZero = CriarEvidenciaValida();
        evidenciaZero.TamanhoBytes = 0;

        var evidenciaAcimaLimite = CriarEvidenciaValida();
        evidenciaAcimaLimite.TamanhoBytes = EvidenciaCondicionante.TamanhoMaximoBytesPadrao + 1;

        // Act
        var errosZero = evidenciaZero.Validar();
        var errosAcima = evidenciaAcimaLimite.Validar();

        // Assert
        Assert.IsNotEmpty(errosZero);
        Assert.IsNotEmpty(errosAcima);
    }

    [TestMethod]
    public void Validar_QuandoExtensaoNaoPermitidaOuAusente_DeveRetornarErro()
    {
        // Arrange
        var semExtensao = CriarEvidenciaValida();
        semExtensao.NomeOriginalArquivo = "arquivo-sem-extensao";

        var extensaoProibida = CriarEvidenciaValida();
        extensaoProibida.NomeOriginalArquivo = "script-malicioso.exe";

        // Act
        var errosSemExtensao = semExtensao.Validar();
        var errosExtensaoProibida = extensaoProibida.Validar();

        // Assert
        Assert.IsNotEmpty(errosSemExtensao);
        Assert.IsNotEmpty(errosExtensaoProibida);
    }

    [TestMethod]
    public void NormalizarCampos_QuandoArtEObservacaoEmBranco_DeveConverterParaNull()
    {
        // Arrange
        var evidencia = CriarEvidenciaValida();
        evidencia.NumeroArt = "   ";
        evidencia.Observacao = "   ";

        // Act
        evidencia.NormalizarCampos();

        // Assert
        Assert.IsNull(evidencia.NumeroArt);
        Assert.IsNull(evidencia.Observacao);
    }

    [TestMethod]
    public void Atualizar_DeveAtualizarApenasNumeroArtEObservacaoNormalizados()
    {
        // Arrange
        var evidencia = CriarEvidenciaValida();
        var nomeOriginal = evidencia.NomeOriginalArquivo;
        var storageOriginal = evidencia.IdentificadorArmazenamento;
        var tamanhoOriginal = evidencia.TamanhoBytes;

        var atualizada = new EvidenciaCondicionante
        {
            NomeOriginalArquivo = "outro-nome.docx",
            IdentificadorArmazenamento = "outro-path",
            TamanhoBytes = 999_999,
            NumeroArt = "  crea-sc-98765  ",
            Observacao = "  Revisado pelo responsável técnico  "
        };

        // Act
        evidencia.Atualizar(atualizada);

        // Assert
        Assert.AreEqual("CREA-SC-98765", evidencia.NumeroArt);
        Assert.AreEqual("Revisado pelo responsável técnico", evidencia.Observacao);
        Assert.AreEqual(nomeOriginal, evidencia.NomeOriginalArquivo);
        Assert.AreEqual(storageOriginal, evidencia.IdentificadorArmazenamento);
        Assert.AreEqual(tamanhoOriginal, evidencia.TamanhoBytes);
    }
}