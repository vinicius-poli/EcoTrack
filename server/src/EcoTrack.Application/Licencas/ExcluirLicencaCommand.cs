using EcoTrack.Application.Common;
using EcoTrack.Domain.Licencas;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Licencas;

public sealed record ExcluirLicencaCommand(Guid Id) : IRequest<Result>;

public sealed class ExcluirLicencaCommandHandler : IRequestHandler<ExcluirLicencaCommand, Result>
{
    private readonly ILicencaRepository _licencaRepository;

    public ExcluirLicencaCommandHandler(ILicencaRepository licencaRepository)
    {
        _licencaRepository = licencaRepository;
    }

    public async Task<Result> Handle(ExcluirLicencaCommand request, CancellationToken cancellationToken)
    {
        // 1. Verifica se a licença existe no contexto do tenant antes de excluir
        var licencaExistente = await _licencaRepository.SelecionarPorIdAsync(request.Id, cancellationToken);
        if (licencaExistente is null)
        {
            return Result.Fail(Erros.Criar("Licença não encontrada.", "Id"));
        }

        // 2. Executa a exclusão no repositório
        var excluida = await _licencaRepository.ExcluirAsync(request.Id, cancellationToken);
        if (!excluida)
        {
            return Result.Fail(Erros.Criar("Não foi possível excluir a licença."));
        }

        return Result.Ok();
    }
}