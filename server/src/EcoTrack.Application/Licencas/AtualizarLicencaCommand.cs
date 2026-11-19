using EcoTrack.Application.Common;
using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Domain.Licencas;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Licencas;

public sealed record AtualizarLicencaRequest(
    Guid EmpreendimentoId,
    string NumeroProcesso,
    string NumeroLicenca,
    string OrgaoEmissor,
    TipoLicenca Tipo,
    DateTime DataEmissao,
    DateTime DataValidade,
    int? DiasAntecedenciaRenovacao,
    StatusLicenca Status
);

public sealed record AtualizarLicencaCommand(Guid Id, AtualizarLicencaRequest Request) : IRequest<Result>;

public sealed class AtualizarLicencaCommandHandler : IRequestHandler<AtualizarLicencaCommand, Result>
{
    private readonly ILicencaRepository _licencaRepository;
    private readonly IEmpreendimentoRepository _empreendimentoRepository;

    public AtualizarLicencaCommandHandler(
        ILicencaRepository licencaRepository,
        IEmpreendimentoRepository empreendimentoRepository)
    {
        _licencaRepository = licencaRepository;
        _empreendimentoRepository = empreendimentoRepository;
    }

    public async Task<Result> Handle(AtualizarLicencaCommand requestModel, CancellationToken cancellationToken)
    {
        var id = requestModel.Id;
        var req = requestModel.Request;

        // 1. Busca a licença existente (já filtrada por TenantId no DbContext)
        var licencaExistente = await _licencaRepository.SelecionarPorIdAsync(id, cancellationToken);
        if (licencaExistente is null)
        {
            return Result.Fail(Erros.Criar("Licença não encontrada.", "Id"));
        }

        // 2. Bloqueia entrada em EmRenovacao via atualização genérica sem protocolo
        if (req.Status == StatusLicenca.EmRenovacao &&
            licencaExistente.Status != StatusLicenca.EmRenovacao)
        {
            return Result.Fail(Erros.Criar(
                "Utilize o endpoint de registro de protocolo para colocar a licença em renovação.",
                "Status"
            ));
        }

        // 3. Atualiza as propriedades na entidade
        licencaExistente.EmpreendimentoId = req.EmpreendimentoId;
        licencaExistente.NumeroProcesso = req.NumeroProcesso;
        licencaExistente.NumeroLicenca = req.NumeroLicenca;
        licencaExistente.OrgaoEmissor = req.OrgaoEmissor;
        licencaExistente.Tipo = req.Tipo;
        licencaExistente.DataEmissao = req.DataEmissao;
        licencaExistente.DataValidade = req.DataValidade;
        licencaExistente.DiasAntecedenciaRenovacao = req.DiasAntecedenciaRenovacao ?? Licenca.DiasAntecedenciaRenovacaoPadrao;
        licencaExistente.Status = req.Status;

        // 4. Valida os campos e regras da entidade (inclui normalização e DataValidade >= DataEmissao)
        var errosValidacao = licencaExistente.Validar();
        if (errosValidacao.Count > 0)
        {
            return Result.Fail(Erros.Validacao(errosValidacao));
        }

        // 5. RF02.1: Garante que o empreendimento informado existe no tenant atual
        var empreendimentoExiste = await _empreendimentoRepository.SelecionarPorIdAsync(
            licencaExistente.EmpreendimentoId,
            cancellationToken);

        if (empreendimentoExiste is null)
        {
            return Result.Fail(Erros.Criar("O empreendimento informado não foi encontrado.", "EmpreendimentoId"));
        }

        // 6. Regra de negócio: verificar se o NumeroLicenca já existe em OUTRA licença
        var licencaJaExiste = await _licencaRepository.ExisteNumeroLicencaAsync(
            licencaExistente.NumeroLicenca,
            ignorarId: id,
            cancellationToken: cancellationToken);

        if (licencaJaExiste)
        {
            return Result.Fail(Erros.Criar("Já existe outra licença cadastrada com este número.", "NumeroLicenca"));
        }

        // 7. Persiste a alteração
        licencaExistente.UpdatedAtUtc = DateTime.UtcNow;

        var atualizado = await _licencaRepository.EditarAsync(id, licencaExistente, cancellationToken);
        if (!atualizado)
        {
            return Result.Fail(Erros.Criar("Não foi possível atualizar a licença."));
        }

        return Result.Ok();
    }
}