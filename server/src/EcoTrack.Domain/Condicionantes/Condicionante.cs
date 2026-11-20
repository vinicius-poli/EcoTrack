using EcoTrack.Domain.Common;
using EcoTrack.Domain.Licencas;

namespace EcoTrack.Domain.Condicionantes;

public sealed class Condicionante : Entity<Condicionante>
{
    public const int DiasAlertaVencimento = 30;

    private readonly List<EvidenciaCondicionante> _evidencias = [];

    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid LicencaId { get; set; }
    public Licenca? Licenca { get; set; }

    public string CodigoItem { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public PeriodicidadeCondicionante Periodicidade { get; set; } = PeriodicidadeCondicionante.Unica;
    public DateTime DataVencimento { get; set; }
    public string? ResponsavelTecnico { get; set; }

    public StatusCondicionante Status { get; set; } = StatusCondicionante.Pendente;

    public IReadOnlyCollection<EvidenciaCondicionante> Evidencias => _evidencias.AsReadOnly();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public override List<string> Validar()
    {
        NormalizarCampos();

        var erros = new List<string>();

        if (LicencaId == Guid.Empty)
            erros.Add("A licença é obrigatória.");

        if (string.IsNullOrWhiteSpace(CodigoItem))
            erros.Add("O código do item é obrigatório.");

        if (string.IsNullOrWhiteSpace(Titulo))
            erros.Add("O título da condicionante é obrigatório.");

        if (string.IsNullOrWhiteSpace(Descricao))
            erros.Add("A descrição detalhada é obrigatória.");

        if (!Enum.IsDefined(Periodicidade))
            erros.Add("A periodicidade é inválida.");

        if (!Enum.IsDefined(Status))
            erros.Add("O status da condicionante é inválido.");

        if (Status == StatusCondicionante.Concluida && _evidencias.Count == 0)
            erros.Add("Não é possível concluir uma condicionante sem ao menos uma evidência anexada.");

        return erros;
    }

    public override void Atualizar(Condicionante entidadeAtualizada)
    {
        entidadeAtualizada.NormalizarCampos();

        LicencaId = entidadeAtualizada.LicencaId;
        CodigoItem = entidadeAtualizada.CodigoItem;
        Titulo = entidadeAtualizada.Titulo;
        Descricao = entidadeAtualizada.Descricao;
        Periodicidade = entidadeAtualizada.Periodicidade;
        DataVencimento = entidadeAtualizada.DataVencimento;
        ResponsavelTecnico = entidadeAtualizada.ResponsavelTecnico;

        // Se tentarem atualizar já como Concluída sem evidência, o Validar() do handler vai barrar
        Status = entidadeAtualizada.Status;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    // RF03.4: Ao anexar a primeira evidência em Pendente, transiciona para EmAndamento
    public List<string> AnexarEvidencia(EvidenciaCondicionante evidencia)
    {
        var erros = new List<string>();

        if (evidencia is null)
        {
            erros.Add("A evidência é obrigatória.");
            return erros;
        }

        evidencia.NormalizarCampos();
        erros.AddRange(evidencia.Validar());
        if (erros.Count > 0)
            return erros;

        evidencia.CondicionanteId = Id;
        evidencia.TenantId = TenantId;
        _evidencias.Add(evidencia);

        if (Status == StatusCondicionante.Pendente)
        {
            Status = StatusCondicionante.EmAndamento;
        }

        UpdatedAtUtc = DateTime.UtcNow;
        return erros;
    }

    // RF03.3: Bloqueio de conclusão sem pelo menos 1 evidência
    public List<string> AlterarStatus(StatusCondicionante novoStatus)
    {
        var erros = new List<string>();

        if (!Enum.IsDefined(novoStatus))
        {
            erros.Add("O status informado é inválido.");
            return erros;
        }

        if (novoStatus == StatusCondicionante.Concluida && _evidencias.Count == 0)
        {
            erros.Add("Não é possível alterar o status para Concluída sem ao menos uma evidência anexada.");
            return erros;
        }

        Status = novoStatus;
        UpdatedAtUtc = DateTime.UtcNow;
        return erros;
    }

    // RF03.5: Cálculo dinâmico do Nível de Risco (Semáforo)
    public NivelRiscoCondicionante CalcularNivelRisco(DateTime? dataReferenciaUtc = null)
    {
        if (Status == StatusCondicionante.Concluida)
            return NivelRiscoCondicionante.Concluido;

        var hoje = (dataReferenciaUtc ?? DateTime.UtcNow).Date;
        var vencimento = DataVencimento.Date;

        if (hoje > vencimento)
            return NivelRiscoCondicionante.Critico;

        var diasRestantes = (vencimento - hoje).Days;

        if (diasRestantes <= DiasAlertaVencimento)
            return NivelRiscoCondicionante.Alerta;

        return NivelRiscoCondicionante.Normal;
    }

    public void NormalizarCampos()
    {
        CodigoItem = CodigoItem.Trim();
        Titulo = Titulo.Trim();
        Descricao = Descricao.Trim();
        ResponsavelTecnico = string.IsNullOrWhiteSpace(ResponsavelTecnico)
            ? null
            : ResponsavelTecnico.Trim();
    }

    public List<string> RemoverEvidencia(Guid evidenciaId)
    {
        var erros = new List<string>();

        var evidencia = _evidencias.FirstOrDefault(e => e.Id == evidenciaId);
        if (evidencia is null)
        {
            erros.Add("Evidência não encontrada na condicionante.");
            return erros;
        }

        _evidencias.Remove(evidencia);

        if (Status == StatusCondicionante.Concluida && _evidencias.Count == 0)
        {
            Status = StatusCondicionante.EmAndamento;
        }

        UpdatedAtUtc = DateTime.UtcNow;
        return erros;
    }
}