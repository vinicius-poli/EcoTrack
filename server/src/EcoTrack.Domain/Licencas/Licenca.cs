using EcoTrack.Domain.Common;
using EcoTrack.Domain.Empreendimentos;

namespace EcoTrack.Domain.Licencas;

public sealed class Licenca : Entity<Licenca>
{
    public const int DiasAntecedenciaRenovacaoPadrao = 120;

    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid EmpreendimentoId { get; set; }
    public Empreendimento? Empreendimento { get; set; }

    public string NumeroProcesso { get; set; } = string.Empty;
    public string NumeroLicenca { get; set; } = string.Empty;
    public string OrgaoEmissor { get; set; } = string.Empty;
    public TipoLicenca Tipo { get; set; }

    public DateTime DataEmissao { get; set; }
    public DateTime DataValidade { get; set; }
    public int DiasAntecedenciaRenovacao { get; set; } = DiasAntecedenciaRenovacaoPadrao;

    public DateTime DataLimiteProtocoloRenovacao =>
        DataValidade.AddDays(-DiasAntecedenciaRenovacao);

    public StatusLicenca Status { get; set; } = StatusLicenca.Vigente;
    public string? NumeroProtocoloRenovacao { get; set; }
    public DateTime? DataProtocoloRenovacao { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public override List<string> Validar()
    {
        NormalizarCampos();

        var erros = new List<string>();

        if (EmpreendimentoId == Guid.Empty)
            erros.Add("O empreendimento é obrigatório.");

        if (string.IsNullOrWhiteSpace(NumeroProcesso))
            erros.Add("O número do processo é obrigatório.");

        if (string.IsNullOrWhiteSpace(NumeroLicenca))
            erros.Add("O número da licença é obrigatório.");

        if (string.IsNullOrWhiteSpace(OrgaoEmissor))
            erros.Add("O órgão emissor é obrigatório.");

        if (!Enum.IsDefined(Tipo))
            erros.Add("O tipo de licença é inválido.");

        if (!Enum.IsDefined(Status))
            erros.Add("O status da licença é inválido.");

        if (DiasAntecedenciaRenovacao < 0)
            erros.Add("Os dias de antecedência para renovação não podem ser negativos.");

        if (DataValidade < DataEmissao)
            erros.Add("A data de validade não pode ser anterior à data de emissão.");

        return erros;
    }

    public override void Atualizar(Licenca entidadeAtualizada)
    {
        entidadeAtualizada.NormalizarCampos();

        EmpreendimentoId = entidadeAtualizada.EmpreendimentoId;
        NumeroProcesso = entidadeAtualizada.NumeroProcesso;
        NumeroLicenca = entidadeAtualizada.NumeroLicenca;
        OrgaoEmissor = entidadeAtualizada.OrgaoEmissor;
        Tipo = entidadeAtualizada.Tipo;
        DataEmissao = entidadeAtualizada.DataEmissao;
        DataValidade = entidadeAtualizada.DataValidade;
        DiasAntecedenciaRenovacao = entidadeAtualizada.DiasAntecedenciaRenovacao;
        Status = entidadeAtualizada.Status;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public List<string> RegistrarProtocoloRenovacao(string numeroProtocolo, DateTime? dataProtocolo = null)
    {
        var erros = new List<string>();

        if (Status == StatusLicenca.Suspensa)
            erros.Add("Não é possível colocar uma licença suspensa em renovação.");

        var protocoloLimpo = numeroProtocolo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(protocoloLimpo))
            erros.Add("O número do protocolo é obrigatório para entrar em renovação.");

        if (erros.Count > 0)
            return erros;

        NumeroProtocoloRenovacao = protocoloLimpo;
        DataProtocoloRenovacao = dataProtocolo ?? DateTime.UtcNow;
        Status = StatusLicenca.EmRenovacao;
        UpdatedAtUtc = DateTime.UtcNow;

        return erros;
    }

    public void NormalizarCampos()
    {
        NumeroProcesso = NumeroProcesso.Trim();
        NumeroLicenca = NumeroLicenca.Trim();
        OrgaoEmissor = OrgaoEmissor.Trim().ToUpperInvariant();
        NumeroProtocoloRenovacao = string.IsNullOrWhiteSpace(NumeroProtocoloRenovacao)
            ? null
            : NumeroProtocoloRenovacao.Trim();
    }
}