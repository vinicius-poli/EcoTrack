using EcoTrack.Domain.Licencas;

namespace EcoTrack.UnitTests.Licencas;

[TestClass]
public sealed class LicencaTests
{
    private static Licenca CriarLicencaValida()
    {
        var emissao = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        return new Licenca
        {
            EmpreendimentoId = Guid.NewGuid(),
            NumeroProcesso = "PROC-2026-001",
            NumeroLicenca = "LAO-2026-010",
            OrgaoEmissor = "IMA",
            Tipo = TipoLicenca.LAO,
            DataEmissao = emissao,
            DataValidade = emissao.AddYears(2),
            DiasAntecedenciaRenovacao = Licenca.DiasAntecedenciaRenovacaoPadrao,
            Status = StatusLicenca.Vigente
        };
    }

    [TestMethod]
    public void Validar_QuandoDadosValidos_DeveRetornarListaVaziaENormalizarCampos()
    {
        // Arrange
        var emissao = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var licenca = new Licenca
        {
            EmpreendimentoId = Guid.NewGuid(),
            NumeroProcesso = "  PROC-2026-045  ",
            NumeroLicenca = "  LAP-2026-009  ",
            OrgaoEmissor = "  ima  ",
            Tipo = TipoLicenca.LAP,
            DataEmissao = emissao,
            DataValidade = emissao.AddYears(1),
            DiasAntecedenciaRenovacao = 90,
            Status = StatusLicenca.Vigente,
            NumeroProtocoloRenovacao = "   "
        };

        // Act
        var erros = licenca.Validar();

        // Assert
        Assert.IsEmpty(erros);
        Assert.AreEqual("PROC-2026-045", licenca.NumeroProcesso);
        Assert.AreEqual("LAP-2026-009", licenca.NumeroLicenca);
        Assert.AreEqual("IMA", licenca.OrgaoEmissor);
        Assert.IsNull(licenca.NumeroProtocoloRenovacao);
    }

    [TestMethod]
    public void Validar_QuandoCamposObrigatoriosInvalidos_DeveRetornarErrosEsperados()
    {
        // Arrange
        var emissao = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var licenca = new Licenca
        {
            EmpreendimentoId = Guid.Empty,
            NumeroProcesso = " ",
            NumeroLicenca = "",
            OrgaoEmissor = "   ",
            Tipo = TipoLicenca.LAI,
            DataEmissao = emissao,
            DataValidade = emissao.AddDays(-1),
            DiasAntecedenciaRenovacao = -5,
            Status = StatusLicenca.Vigente
        };

        // Act
        var erros = licenca.Validar();

        // Assert
        Assert.Contains("O empreendimento é obrigatório.", erros);
        Assert.Contains("O número do processo é obrigatório.", erros);
        Assert.Contains("O número da licença é obrigatório.", erros);
        Assert.Contains("O órgão emissor é obrigatório.", erros);
        Assert.Contains("Os dias de antecedência para renovação não podem ser negativos.", erros);
        Assert.Contains("A data de validade não pode ser anterior à data de emissão.", erros);
    }

    [TestMethod]
    public void DataLimiteProtocoloRenovacao_DeveSubtrairDiasDeAntecedenciaDaDataValidade()
    {
        // Arrange
        var licenca = CriarLicencaValida();
        licenca.DataValidade = new DateTime(2028, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        licenca.DiasAntecedenciaRenovacao = 120;

        // Act
        var limite = licenca.DataLimiteProtocoloRenovacao;

        // Assert
        Assert.AreEqual(new DateTime(2028, 1, 2, 0, 0, 0, DateTimeKind.Utc), limite);
    }

    [TestMethod]
    public void RegistrarProtocoloRenovacao_QuandoValido_DeveAtualizarProtocoloEStatusEmRenovacao()
    {
        // Arrange
        var licenca = CriarLicencaValida();
        var dataProtocolo = new DateTime(2027, 10, 15, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var erros = licenca.RegistrarProtocoloRenovacao("  PROT-998877  ", dataProtocolo);

        // Assert
        Assert.IsEmpty(erros);
        Assert.AreEqual("PROT-998877", licenca.NumeroProtocoloRenovacao);
        Assert.AreEqual(dataProtocolo, licenca.DataProtocoloRenovacao);
        Assert.AreEqual(StatusLicenca.EmRenovacao, licenca.Status);
    }

    [TestMethod]
    public void RegistrarProtocoloRenovacao_QuandoLicencaSuspensa_DeveRetornarErroENaoAlterarStatus()
    {
        // Arrange
        var licenca = CriarLicencaValida();
        licenca.Status = StatusLicenca.Suspensa;

        // Act
        var erros = licenca.RegistrarProtocoloRenovacao("PROT-123");

        // Assert
        Assert.Contains("Não é possível colocar uma licença suspensa em renovação.", erros);
        Assert.AreEqual(StatusLicenca.Suspensa, licenca.Status);
        Assert.IsNull(licenca.NumeroProtocoloRenovacao);
    }

    [TestMethod]
    public void RegistrarProtocoloRenovacao_QuandoProtocoloVazio_DeveRetornarErro()
    {
        // Arrange
        var licenca = CriarLicencaValida();

        // Act
        var erros = licenca.RegistrarProtocoloRenovacao("   ");

        // Assert
        Assert.Contains("O número do protocolo é obrigatório para entrar em renovação.", erros);
        Assert.AreEqual(StatusLicenca.Vigente, licenca.Status);
    }

    [TestMethod]
    public void Atualizar_DeveAtualizarCamposENormalizarOrgaoEmissor()
    {
        // Arrange
        var licenca = CriarLicencaValida();
        var novoEmpreendimentoId = Guid.NewGuid();
        var novaEmissao = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var atualizada = new Licenca
        {
            EmpreendimentoId = novoEmpreendimentoId,
            NumeroProcesso = "  PROC-2026-777  ",
            NumeroLicenca = "  AuA-2026-003  ",
            OrgaoEmissor = "  fatma  ",
            Tipo = TipoLicenca.AuA,
            DataEmissao = novaEmissao,
            DataValidade = novaEmissao.AddMonths(6),
            DiasAntecedenciaRenovacao = 60,
            Status = StatusLicenca.Vigente
        };

        // Act
        licenca.Atualizar(atualizada);

        // Assert
        Assert.AreEqual(novoEmpreendimentoId, licenca.EmpreendimentoId);
        Assert.AreEqual("PROC-2026-777", licenca.NumeroProcesso);
        Assert.AreEqual("AuA-2026-003", licenca.NumeroLicenca);
        Assert.AreEqual("FATMA", licenca.OrgaoEmissor);
        Assert.AreEqual(TipoLicenca.AuA, licenca.Tipo);
        Assert.AreEqual(60, licenca.DiasAntecedenciaRenovacao);
    }

    [TestMethod]
    [DataRow(TipoLicenca.LAP)]
    [DataRow(TipoLicenca.LAI)]
    [DataRow(TipoLicenca.LAO)]
    [DataRow(TipoLicenca.AuA)]
    public void Validar_QuandoTipoLicencaValido_NaoDeveRetornarErroDeTipo(TipoLicenca tipo)
    {
        // Arrange
        var licenca = CriarLicencaValida();
        licenca.Tipo = tipo;

        // Act
        var erros = licenca.Validar();

        // Assert
        Assert.DoesNotContain("O tipo de licença é inválido.", erros);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(5)]
    [DataRow(-1)]
    public void Validar_QuandoTipoLicencaForaDoEnum_DeveRetornarErroDeTipo(int valorTipo)
    {
        // Arrange
        var licenca = CriarLicencaValida();
        licenca.Tipo = (TipoLicenca)valorTipo;

        // Act
        var erros = licenca.Validar();

        // Assert
        Assert.Contains("O tipo de licença é inválido.", erros);
    }

    [TestMethod]
    [DataRow(StatusLicenca.Vigente)]
    [DataRow(StatusLicenca.EmRenovacao)]
    [DataRow(StatusLicenca.Suspensa)]
    [DataRow(StatusLicenca.Vencida)]
    [DataRow(StatusLicenca.Cancelada)]
    public void Validar_QuandoStatusLicencaValido_NaoDeveRetornarErroDeStatus(StatusLicenca status)
    {
        // Arrange
        var licenca = CriarLicencaValida();
        licenca.Status = status;

        // Act
        var erros = licenca.Validar();

        // Assert
        Assert.DoesNotContain("O status da licença é inválido.", erros);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(6)]
    [DataRow(-1)]
    public void Validar_QuandoStatusLicencaForaDoEnum_DeveRetornarErroDeStatus(int valorStatus)
    {
        // Arrange
        var licenca = CriarLicencaValida();
        licenca.Status = (StatusLicenca)valorStatus;

        // Act
        var erros = licenca.Validar();

        // Assert
        Assert.Contains("O status da licença é inválido.", erros);
    }
}