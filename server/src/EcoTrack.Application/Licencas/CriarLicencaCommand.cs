using EcoTrack.Application.Common;
using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Domain.Licencas;
using FluentResults;
using MediatR;

namespace EcoTrack.Application.Licencas;

public sealed record CriarLicencaRequest(
    Guid EmpreendimentoId,
    string NumeroProcesso,
    string NumeroLicenca,
    string OrgaoEmissor,
    TipoLicenca Tipo,
    DateTime DataEmissao,
    DateTime DataValidade,
    int? DiasAntecedenciaRenovacao = null
);

public sealed record CriarLicencaCommand(CriarLicencaRequest Request) : IRequest<Result<Guid>>;

public sealed class CriarLicencaCommandHandler : IRequestHandler<CriarLicencaCommand, Result<Guid>>
{
    private readonly ILicencaRepository _licencaRepository;
    private readonly IEmpreendimentoRepository _empreendimentoRepository;
    private readonly IUserContext _userContext;

    public CriarLicencaCommandHandler(
        ILicencaRepository licencaRepository,
        IEmpreendimentoRepository empreendimentoRepository,
        IUserContext userContext)
    {
        _licencaRepository = licencaRepository;
        _empreendimentoRepository = empreendimentoRepository;
        _userContext = userContext;
    }

    public async Task<Result<Guid>> Handle(CriarLicencaCommand requestModel, CancellationToken cancellationToken)
    {
        var req = requestModel.Request;

        if (_userContext.TenantId is null)
        {
            return Result.Fail<Guid>(Erros.Criar("Tenant não identificado na sessão.", "TenantId"));
        }

        var licenca = new Licenca
        {
            EmpreendimentoId = req.EmpreendimentoId,
            NumeroProcesso = req.NumeroProcesso,
            NumeroLicenca = req.NumeroLicenca,
            OrgaoEmissor = req.OrgaoEmissor,
            Tipo = req.Tipo,
            DataEmissao = req.DataEmissao,
            DataValidade = req.DataValidade,
            DiasAntecedenciaRenovacao = req.DiasAntecedenciaRenovacao ?? Licenca.DiasAntecedenciaRenovacaoPadrao,
            TenantId = _userContext.TenantId.Value,
            UserId = _userContext.UserId ?? Guid.Empty
        };

        // 1. Validação da entidade (normaliza campos primeiro)
        var errosValidacao = licenca.Validar();
        if (errosValidacao.Count > 0)
        {
            return Result.Fail<Guid>(Erros.Validacao(errosValidacao));
        }

        // 2. RF02.1: Empreendimento precisa existir (já filtrado pelo TenantId no DbContext)
        var empreendimentoExiste = await _empreendimentoRepository.SelecionarPorIdAsync(
            licenca.EmpreendimentoId,
            cancellationToken);

        if (empreendimentoExiste is null)
        {
            return Result.Fail<Guid>(Erros.Criar("O empreendimento informado não foi encontrado.", "EmpreendimentoId"));
        }

        // 3. Regra de negócio: Número de licença duplicado
        var licencaJaExiste = await _licencaRepository.ExisteNumeroLicencaAsync(
            licenca.NumeroLicenca,
            cancellationToken: cancellationToken);

        if (licencaJaExiste)
        {
            return Result.Fail<Guid>(Erros.Criar("Já existe uma licença cadastrada com este número.", "NumeroLicenca"));
        }

        // 4. Persistência (DataLimiteProtocoloRenovacao é calculada via DataValidade e DiasAntecedenciaRenovacao)
        await _licencaRepository.CadastrarAsync(licenca, cancellationToken);

        return Result.Ok(licenca.Id);
    }
}