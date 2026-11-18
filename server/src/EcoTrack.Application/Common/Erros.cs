using FluentResults;

namespace EcoTrack.Application.Common;

public static class Erros
{
    public static Error Criar(string mensagem, string? campo = null)
    {
        var erro = new Error(mensagem);

        if (campo is not null)
            erro.WithMetadata("Campo", campo);

        return erro;
    }

    public static IEnumerable<Error> Validacao(IEnumerable<string> mensagens)
    {
        return mensagens.Select(msg => Criar(msg));
    }
}