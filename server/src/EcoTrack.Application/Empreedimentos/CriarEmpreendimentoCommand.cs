using EcoTrack.Application.Common;
using EcoTrack.Domain.Empreendimentos;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Empreendimentos;

public sealed record CriarEmpreendimentoRequest(
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string Municipio,
    string Uf,
    PorteEmpreendimento Porte,
    StatusLicenciamento StatusLicenciamento
);

public sealed record CriarEmpreendimentoCommand(CriarEmpreendimentoRequest Request) : IRequest<Result<Guid>>;

public sealed class CriarEmpreendimentoCommandHandler : IRequestHandler<CriarEmpreendimentoCommand, Result<Guid>>
{
    private readonly IEmpreendimentoRepository _empreendimentoRepository;

    public CriarEmpreendimentoCommandHandler(IEmpreendimentoRepository empreendimentoRepository)
    {
        _empreendimentoRepository = empreendimentoRepository;
    }

    public async Task<Result<Guid>> Handle(CriarEmpreendimentoCommand requestModel, CancellationToken cancellationToken)
    {
        var req = requestModel.Request;

        var empreendimento = new Empreendimento
        {
            RazaoSocial = req.RazaoSocial,
            NomeFantasia = req.NomeFantasia,
            Cnpj = req.Cnpj,
            Municipio = req.Municipio,
            Uf = req.Uf,
            Porte = req.Porte,
            StatusLicenciamento = req.StatusLicenciamento
        };

        // Validação da entidade
        var errosValidacao = empreendimento.Validar();
        if (errosValidacao.Count > 0)
        {
            return Result.Fail<Guid>(Erros.Validacao(errosValidacao));
        }

        // Regra de negócio: CNPJ duplicado
        var cnpjJaExiste = await _empreendimentoRepository.ExisteCnpjAsync(empreendimento.Cnpj, cancellationToken: cancellationToken);
        if (cnpjJaExiste)
        {
            return Result.Fail<Guid>(Erros.Criar("Já existe um empreendimento cadastrado com este CNPJ.", "Cnpj"));
        }

        // Persistência
        await _empreendimentoRepository.CadastrarAsync(empreendimento, cancellationToken);

        return Result.Ok(empreendimento.Id);
    }
}