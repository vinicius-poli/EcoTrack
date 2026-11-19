using EcoTrack.Application.Common;
using EcoTrack.Domain.Licencas;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Licencas;

public sealed record RegistrarProtocoloRenovacaoRequest(
    string NumeroProtocolo,
    DateTime? DataProtocolo = null
);

public sealed record RegistrarProtocoloRenovacaoCommand(
    Guid Id,
    RegistrarProtocoloRenovacaoRequest Request
) : IRequest<Result>;

public sealed class RegistrarProtocoloRenovacaoCommandHandler
    : IRequestHandler<RegistrarProtocoloRenovacaoCommand, Result>
{
    private readonly ILicencaRepository _licencaRepository;

    public RegistrarProtocoloRenovacaoCommandHandler(ILicencaRepository licencaRepository)
    {
        _licencaRepository = licencaRepository;
    }

    public async Task<Result> Handle(
        RegistrarProtocoloRenovacaoCommand requestModel,
        CancellationToken cancellationToken)
    {
        var id = requestModel.Id;
        var req = requestModel.Request;

        // 1. Busca a licença existente (já filtrada por TenantId no DbContext)
        var licenca = await _licencaRepository.SelecionarPorIdAsync(id, cancellationToken);
        if (licenca is null)
        {
            return Result.Fail(Erros.Criar("Licença não encontrada.", "Id"));
        }

        // 2. Aplica regra de domínio (RF02.4): bloqueia se Suspensa e exige protocolo
        var errosDominio = licenca.RegistrarProtocoloRenovacao(req.NumeroProtocolo, req.DataProtocolo);
        if (errosDominio.Count > 0)
        {
            return Result.Fail(Erros.Validacao(errosDominio));
        }

        // 3. Persiste a mudança de status e dados do protocolo
        var atualizado = await _licencaRepository.EditarAsync(id, licenca, cancellationToken);
        if (!atualizado)
        {
            return Result.Fail(Erros.Criar("Não foi possível registrar o protocolo de renovação."));
        }

        return Result.Ok();
    }
}