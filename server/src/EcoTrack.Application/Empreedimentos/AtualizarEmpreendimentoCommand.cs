using EcoTrack.Application.Common;
using EcoTrack.Domain.Empreendimentos;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Empreendimentos;

public sealed record AtualizarEmpreendimentoRequest(
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string Municipio,
    string Uf,
    PorteEmpreendimento Porte,
    StatusLicenciamento StatusLicenciamento
);

public sealed record AtualizarEmpreendimentoCommand(Guid Id, AtualizarEmpreendimentoRequest Request) : IRequest<Result>;

public sealed class AtualizarEmpreendimentoCommandHandler : IRequestHandler<AtualizarEmpreendimentoCommand, Result>
{
    private readonly IEmpreendimentoRepository _empreendimentoRepository;

    public AtualizarEmpreendimentoCommandHandler(IEmpreendimentoRepository empreendimentoRepository)
    {
        _empreendimentoRepository = empreendimentoRepository;
    }

    public async Task<Result> Handle(AtualizarEmpreendimentoCommand requestModel, CancellationToken cancellationToken)
    {
        var id = requestModel.Id;
        var req = requestModel.Request;

        // 1. Busca o empreendimento existente
        var empreendimentoExistente = await _empreendimentoRepository.SelecionarPorIdAsync(id, cancellationToken);
        if (empreendimentoExistente is null)
        {
            return Result.Fail(Erros.Criar("Empreendimento não encontrado.", "Id"));
        }

        // 2. Atualiza as propriedades na entidade (ou cria uma instância temporária para validar)
        empreendimentoExistente.RazaoSocial = req.RazaoSocial;
        empreendimentoExistente.NomeFantasia = req.NomeFantasia;
        empreendimentoExistente.Cnpj = req.Cnpj;
        empreendimentoExistente.Municipio = req.Municipio;
        empreendimentoExistente.Uf = req.Uf;
        empreendimentoExistente.Porte = req.Porte;
        empreendimentoExistente.StatusLicenciamento = req.StatusLicenciamento;

        // 3. Valida os campos da entidade
        var errosValidacao = empreendimentoExistente.Validar();
        if (errosValidacao.Count > 0)
        {
            return Result.Fail(Erros.Validacao(errosValidacao));
        }

        // 4. Regra de negócio: verificar se o CNPJ alterado já existe em OUTRO empreendimento
        var cnpjJaExiste = await _empreendimentoRepository.ExisteCnpjAsync(empreendimentoExistente.Cnpj, ignorarId: id, cancellationToken: cancellationToken);
        if (cnpjJaExiste)
        {
            return Result.Fail(Erros.Criar("Já existe outro empreendimento cadastrado com este CNPJ.", "Cnpj"));
        }

        // 5. Persiste a alteração
        var atualizado = await _empreendimentoRepository.EditarAsync(id, empreendimentoExistente, cancellationToken);
        if (!atualizado)
        {
            return Result.Fail(Erros.Criar("Não foi possível atualizar o empreendimento."));
        }

        return Result.Ok();
    }
}