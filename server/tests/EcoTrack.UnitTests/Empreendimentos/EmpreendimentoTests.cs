using EcoTrack.Domain.Empreendimentos;

namespace EcoTrack.UnitTests.Empreendimentos;

[TestClass]
public sealed class EmpreendimentoTests
{
    private static Empreendimento CriarEmpreendimentoValido() => new()
    {
        RazaoSocial = "Empresa Teste Ltda",
        NomeFantasia = "Empresa Teste",
        Cnpj = "45678912378952",
        Municipio = "Lages",
        Uf = "SC",
        Porte = PorteEmpreendimento.Micro,
        StatusLicenciamento = StatusLicenciamento.EmAnalise
    };

    [TestMethod]
    public void Validar_QuandoDadosValidos_DeveRetornarListaVaziaENormalizarCampos()
    {
        // Arrange
        var empreendimento = new Empreendimento
        {
            RazaoSocial = "  Empresa Teste Ltda  ",
            NomeFantasia = "  Empresa Teste  ",
            Cnpj = "45.678.912/3789-52",
            Municipio = "  Lages  ",
            Uf = " sc ",
            Porte = PorteEmpreendimento.Micro,
            StatusLicenciamento = StatusLicenciamento.EmAnalise
        };

        // Act
        var erros = empreendimento.Validar();

        // Assert
        Assert.IsEmpty(erros);
        Assert.AreEqual("Empresa Teste Ltda", empreendimento.RazaoSocial);
        Assert.AreEqual("Empresa Teste", empreendimento.NomeFantasia);
        Assert.AreEqual("45678912378952", empreendimento.Cnpj);
        Assert.AreEqual("Lages", empreendimento.Municipio);
        Assert.AreEqual("SC", empreendimento.Uf);
    }

    [TestMethod]
    public void Validar_QuandoCnpjNaoTem14Digitos_DeveRetornarErroDeCnpj()
    {
        // Arrange
        var empreendimento = CriarEmpreendimentoValido();
        empreendimento.Cnpj = "123456";

        // Act
        var erros = empreendimento.Validar();

        // Assert
        Assert.Contains("O CNPJ deve conter 14 dígitos.", erros);
    }

    [TestMethod]
    public void Validar_QuandoCamposObrigatoriosVaziosEUfInvalida_DeveRetornarErrosEsperados()
    {
        // Arrange
        var empreendimento = new Empreendimento
        {
            RazaoSocial = "   ",
            NomeFantasia = "",
            Cnpj = "123",
            Municipio = " ",
            Uf = "S",
            Porte = PorteEmpreendimento.Micro,
            StatusLicenciamento = StatusLicenciamento.EmAnalise
        };

        // Act
        var erros = empreendimento.Validar();

        // Assert
        Assert.Contains("A razão social é obrigatória.", erros);
        Assert.Contains("O nome fantasia é obrigatório.", erros);
        Assert.Contains("O CNPJ deve conter 14 dígitos.", erros);
        Assert.Contains("O município é obrigatório.", erros);
        Assert.Contains("A UF deve conter 2 caracteres.", erros);
    }

    [TestMethod]
    public void Validar_QuandoPorteOuStatusInvalido_DeveRetornarErroDeEnum()
    {
        // Arrange
        var empreendimento = CriarEmpreendimentoValido();
        empreendimento.Porte = (PorteEmpreendimento)999;
        empreendimento.StatusLicenciamento = (StatusLicenciamento)999;

        // Act
        var erros = empreendimento.Validar();

        // Assert
        Assert.Contains("O porte do empreendimento é inválido.", erros);
        Assert.Contains("O status de licenciamento é inválido.", erros);
    }

    [TestMethod]
    public void Atualizar_DeveAtualizarCamposPermitidosENormalizarValores()
    {
        // Arrange
        var original = CriarEmpreendimentoValido();
        var cnpjOriginal = original.Cnpj;
        var atualizacao = new Empreendimento
        {
            RazaoSocial = "  Nova Razão Social S/A  ",
            NomeFantasia = "  Nova Unidade  ",
            Cnpj = "99999999999999",
            Municipio = "  Florianópolis  ",
            Uf = "sc",
            Porte = PorteEmpreendimento.Micro,
            StatusLicenciamento = StatusLicenciamento.EmAnalise
        };

        // Act
        original.Atualizar(atualizacao);

        // Assert
        Assert.AreEqual("Nova Razão Social S/A", original.RazaoSocial);
        Assert.AreEqual("Nova Unidade", original.NomeFantasia);
        Assert.AreEqual("Florianópolis", original.Municipio);
        Assert.AreEqual("SC", original.Uf);
        Assert.AreEqual(cnpjOriginal, original.Cnpj);
    }

    [TestMethod]
    [DataRow(PorteEmpreendimento.Micro)]
    [DataRow(PorteEmpreendimento.Pequeno)]
    [DataRow(PorteEmpreendimento.Medio)]
    [DataRow(PorteEmpreendimento.Grande)]
    [DataRow(PorteEmpreendimento.Excepcional)]
    public void Validar_QuandoPorteValido_NaoDeveRetornarErroDePorte(PorteEmpreendimento porte)
    {
        // Arrange
        var empreendimento = CriarEmpreendimentoValido();
        empreendimento.Porte = porte;

        // Act
        var erros = empreendimento.Validar();

        // Assert
        Assert.DoesNotContain("O porte do empreendimento é inválido.", erros);
    }

    [TestMethod]
    [DataRow(StatusLicenciamento.EmAnalise)]
    [DataRow(StatusLicenciamento.Licenciado)]
    [DataRow(StatusLicenciamento.Suspenso)]
    [DataRow(StatusLicenciamento.Vencido)]
    [DataRow(StatusLicenciamento.Inativo)]
    public void Validar_QuandoStatusLicenciamentoValido_NaoDeveRetornarErroDeStatus(
        StatusLicenciamento status)
    {
        // Arrange
        var empreendimento = CriarEmpreendimentoValido();
        empreendimento.StatusLicenciamento = status;

        // Act
        var erros = empreendimento.Validar();

        // Assert
        Assert.DoesNotContain("O status de licenciamento é inválido.", erros);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(6)]
    [DataRow(-1)]
    public void Validar_QuandoPorteForaDoEnum_DeveRetornarErroDePorte(int valorPorte)
    {
        // Arrange
        var empreendimento = CriarEmpreendimentoValido();
        empreendimento.Porte = (PorteEmpreendimento)valorPorte;

        // Act
        var erros = empreendimento.Validar();

        // Assert
        Assert.Contains("O porte do empreendimento é inválido.", erros);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(6)]
    [DataRow(-1)]
    public void Validar_QuandoStatusLicenciamentoForaDoEnum_DeveRetornarErroDeStatus(int valorStatus)
    {
        // Arrange
        var empreendimento = CriarEmpreendimentoValido();
        empreendimento.StatusLicenciamento = (StatusLicenciamento)valorStatus;

        // Act
        var erros = empreendimento.Validar();

        // Assert
        Assert.Contains("O status de licenciamento é inválido.", erros);
    }
}