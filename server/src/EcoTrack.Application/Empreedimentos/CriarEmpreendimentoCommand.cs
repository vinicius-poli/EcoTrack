using EcoTrack.Application.Common;
using EcoTrack.Domain.Auth;
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
    private readonly IUserContext _userContext; // <-- Injetamos o contexto aqui

    public CriarEmpreendimentoCommandHandler(
        IEmpreendimentoRepository empreendimentoRepository, 
        IUserContext userContext)
    {
        _empreendimentoRepository = empreendimentoRepository;
        _userContext = userContext;
    }

    public async Task<Result<Guid>> Handle(CriarEmpreendimentoCommand requestModel, CancellationToken cancellationToken)
    {
        var req = requestModel.Request;

        // Garantir que temos o TenantId da sessão atual
        if (_userContext.TenantId is null)
        {
            return Result.Fail<Guid>(Erros.Criar("Tenant não identificado na sessão.", "TenantId"));
        }

        var empreendimento = new Empreendimento
        {
            RazaoSocial = req.RazaoSocial,
            NomeFantasia = req.NomeFantasia,
            Cnpj = req.Cnpj,
            Municipio = req.Municipio,
            Uf = req.Uf,
            Porte = req.Porte,
            StatusLicenciamento = req.StatusLicenciamento,
            TenantId = _userContext.TenantId.Value,
            UserId = _userContext.UserId ?? Guid.Empty
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