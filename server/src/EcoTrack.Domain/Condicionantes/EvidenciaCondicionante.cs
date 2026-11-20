using EcoTrack.Domain.Common;

namespace EcoTrack.Domain.Condicionantes;

public sealed class EvidenciaCondicionante : Entity<EvidenciaCondicionante>
{
    public const long TamanhoMaximoBytesPadrao = 10 * 1024 * 1024; // 10 MB

    public static readonly string[] ExtensoesPermitidas =
    [
        ".pdf", ".jpg", ".jpeg", ".png", ".xlsx", ".docx"
    ];

    public Guid CondicionanteId { get; set; }
    public Condicionante? Condicionante { get; set; }
    public Guid TenantId { get; set; }
    public Guid EnviadoPorUserId { get; set; }

    public string NomeOriginalArquivo { get; set; } = string.Empty;
    public string IdentificadorArmazenamento { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public string? NumeroArt { get; set; }
    public string? Observacao { get; set; }
    public DateTime DataUploadUtc { get; set; } = DateTime.UtcNow;

    public override List<string> Validar()
    {
        NormalizarCampos();

        var erros = new List<string>();

        if (string.IsNullOrWhiteSpace(NomeOriginalArquivo))
            erros.Add("O nome original do arquivo é obrigatório.");

        if (string.IsNullOrWhiteSpace(IdentificadorArmazenamento))
            erros.Add("O identificador de armazenamento é obrigatório.");

        if (string.IsNullOrWhiteSpace(ContentType))
            erros.Add("O Content-Type do arquivo é obrigatório.");

        if (TamanhoBytes <= 0)
            erros.Add("O tamanho do arquivo deve ser maior que zero.");
        else if (TamanhoBytes > TamanhoMaximoBytesPadrao)
            erros.Add("O arquivo excede o tamanho máximo permitido de 10 MB.");

        if (!string.IsNullOrWhiteSpace(NomeOriginalArquivo))
{
    var extensao = Path.GetExtension(NomeOriginalArquivo).ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(extensao) || !ExtensoesPermitidas.Contains(extensao))
        erros.Add("Formato de arquivo não permitido. Use .pdf, .jpg, .jpeg, .png, .xlsx ou .docx.");
}

        return erros;
    }

    public override void Atualizar(EvidenciaCondicionante entidadeAtualizada)
    {
        entidadeAtualizada.NormalizarCampos();
        NumeroArt = entidadeAtualizada.NumeroArt;
        Observacao = entidadeAtualizada.Observacao;
    }

    public void NormalizarCampos()
    {
        NomeOriginalArquivo = NomeOriginalArquivo.Trim();
        IdentificadorArmazenamento = IdentificadorArmazenamento.Trim();
        ContentType = ContentType.Trim();
        NumeroArt = string.IsNullOrWhiteSpace(NumeroArt) ? null : NumeroArt.Trim().ToUpperInvariant();
        Observacao = string.IsNullOrWhiteSpace(Observacao) ? null : Observacao.Trim();
    }
}