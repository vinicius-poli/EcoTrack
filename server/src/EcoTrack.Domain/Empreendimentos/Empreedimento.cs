using EcoTrack.Domain.Common;

namespace EcoTrack.Domain.Empreendimentos;

public sealed class Empreendimento : Entity<Empreendimento>
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public PorteEmpreendimento Porte { get; set; }
    public StatusLicenciamento StatusLicenciamento { get; set; } = StatusLicenciamento.EmAnalise;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public override List<string> Validar()
{
    NormalizarCampos();

    var erros = new List<string>();

    if (string.IsNullOrWhiteSpace(RazaoSocial))
        erros.Add("A razão social é obrigatória.");

    if (string.IsNullOrWhiteSpace(NomeFantasia))
        erros.Add("O nome fantasia é obrigatório.");

    if (Cnpj.Length != 14)
        erros.Add("O CNPJ deve conter 14 dígitos.");

    if (string.IsNullOrWhiteSpace(Municipio))
        erros.Add("O município é obrigatório.");

    if (Uf.Length != 2)
        erros.Add("A UF deve conter 2 caracteres.");

    if (!Enum.IsDefined(Porte))
        erros.Add("O porte do empreendimento é inválido.");

    if (!Enum.IsDefined(StatusLicenciamento))
        erros.Add("O status de licenciamento é inválido.");

    return erros;
}

public override void Atualizar(Empreendimento entidadeAtualizada)
{
    entidadeAtualizada.NormalizarCampos();

    RazaoSocial = entidadeAtualizada.RazaoSocial;
    NomeFantasia = entidadeAtualizada.NomeFantasia;
    Municipio = entidadeAtualizada.Municipio;
    Uf = entidadeAtualizada.Uf;
    Porte = entidadeAtualizada.Porte;
    StatusLicenciamento = entidadeAtualizada.StatusLicenciamento;
    UpdatedAtUtc = DateTime.UtcNow;
}

    public void NormalizarCampos()
    {
        RazaoSocial = RazaoSocial.Trim();
        NomeFantasia = NomeFantasia.Trim();
        Cnpj = NormalizarCnpj(Cnpj);
        Municipio = Municipio.Trim();
        Uf = NormalizarUf(Uf);
    }

    private static string NormalizarCnpj(string? cnpj) =>
        string.IsNullOrWhiteSpace(cnpj)
            ? string.Empty
            : new string(cnpj.Where(char.IsDigit).ToArray());

    private static string NormalizarUf(string? uf) =>
        string.IsNullOrWhiteSpace(uf)
            ? string.Empty
            : uf.Trim().ToUpperInvariant();
}