using EcoTrack.Application.Common;
using EcoTrack.Domain.Empreendimentos;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Empreendimentos;

public sealed record ObterEmpreendimentoPorIdQuery(Guid Id) : IRequest<Result<EmpreendimentoDto>>;

public sealed class ObterEmpreendimentoPorIdQueryHandler : IRequestHandler<ObterEmpreendimentoPorIdQuery, Result<EmpreendimentoDto>>
{
    private readonly IEmpreendimentoRepository _empreendimentoRepository;

    public ObterEmpreendimentoPorIdQueryHandler(IEmpreendimentoRepository empreendimentoRepository)
    {
        _empreendimentoRepository = empreendimentoRepository;
    }

    public async Task<Result<EmpreendimentoDto>> Handle(ObterEmpreendimentoPorIdQuery request, CancellationToken cancellationToken)
    {
        var empreendimento = await _empreendimentoRepository.SelecionarPorIdAsync(request.Id, cancellationToken);
        if (empreendimento is null)
        {
            return Result.Fail<EmpreendimentoDto>(Erros.Criar("Empreendimento não encontrado.", "Id"));
        }

        return Result.Ok(DtoMapper.ToDto(empreendimento));
    }
}