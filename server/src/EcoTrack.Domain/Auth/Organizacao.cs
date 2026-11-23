namespace EcoTrack.Domain.Auth;

public sealed class Organizacao
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public DateTime CriadoEmUtc { get; private set; }

    private Organizacao() { }

    public Organizacao(Guid id, string nome, DateTime criadoEmUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("O identificador da organização é obrigatório.", nameof(id));

        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome da organização é obrigatório.", nameof(nome));

        Id = id;
        Nome = nome.Trim();
        CriadoEmUtc = criadoEmUtc;
    }

    public void Renomear(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new ArgumentException("O nome da organização é obrigatório.", nameof(novoNome));

        Nome = novoNome.Trim();
    }
}