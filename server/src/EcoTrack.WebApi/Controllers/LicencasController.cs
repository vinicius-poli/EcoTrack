using EcoTrack.Application.Licencas;
using EcoTrack.Domain.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoTrack.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LicencasController : ControllerBase
{
    private const string PerfisEscrita =
        $"{nameof(PerfilUsuario.Gestor)},{nameof(PerfilUsuario.Administrador)}";

    private const string PerfilExclusao =
        nameof(PerfilUsuario.Administrador);

    private readonly IMediator _mediator;

    public LicencasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var query = new ListarLicencasQuery();
        var resultado = await _mediator.Send(query, cancellationToken);

        if (resultado.IsFailed)
            return BadRequest(resultado.Errors);

        return Ok(resultado.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var query = new ObterLicencaPorIdQuery(id);
        var resultado = await _mediator.Send(query, cancellationToken);

        if (resultado.IsFailed)
        {
            if (resultado.Errors.Any(e => e.Message.Contains("não encontrado", StringComparison.OrdinalIgnoreCase) ||
                                          e.Message.Contains("não encontrada", StringComparison.OrdinalIgnoreCase)))
                return NotFound(resultado.Errors);

            return BadRequest(resultado.Errors);
        }

        return Ok(resultado.Value);
    }

    [HttpGet("por-empreendimento/{empreendimentoId:guid}")]
    public async Task<IActionResult> ListarPorEmpreendimento(Guid empreendimentoId, CancellationToken cancellationToken)
    {
        var query = new ListarLicencasPorEmpreendimentoQuery(empreendimentoId);
        var resultado = await _mediator.Send(query, cancellationToken);

        if (resultado.IsFailed)
        {
            if (resultado.Errors.Any(e => e.Message.Contains("não encontrado", StringComparison.OrdinalIgnoreCase) ||
                                          e.Message.Contains("não encontrada", StringComparison.OrdinalIgnoreCase)))
                return NotFound(resultado.Errors);

            return BadRequest(resultado.Errors);
        }

        return Ok(resultado.Value);
    }

    [HttpPost]
    [Authorize(Roles = PerfisEscrita)]
    public async Task<IActionResult> Criar([FromBody] CriarLicencaRequest request, CancellationToken cancellationToken)
    {
        var command = new CriarLicencaCommand(request);
        var resultado = await _mediator.Send(command, cancellationToken);

        if (resultado.IsFailed)
            return BadRequest(resultado.Errors);

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Value }, resultado.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = PerfisEscrita)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarLicencaRequest request, CancellationToken cancellationToken)
    {
        var command = new AtualizarLicencaCommand(id, request);
        var resultado = await _mediator.Send(command, cancellationToken);

        if (resultado.IsFailed)
        {
            if (resultado.Errors.Any(e => e.Message.Contains("não encontrado", StringComparison.OrdinalIgnoreCase) ||
                                          e.Message.Contains("não encontrada", StringComparison.OrdinalIgnoreCase)))
                return NotFound(resultado.Errors);

            return BadRequest(resultado.Errors);
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/protocolo-renovacao")]
    [Authorize(Roles = PerfisEscrita)]
    public async Task<IActionResult> RegistrarProtocoloRenovacao(
        Guid id,
        [FromBody] RegistrarProtocoloRenovacaoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegistrarProtocoloRenovacaoCommand(id, request);
        var resultado = await _mediator.Send(command, cancellationToken);

        if (resultado.IsFailed)
        {
            if (resultado.Errors.Any(e => e.Message.Contains("não encontrado", StringComparison.OrdinalIgnoreCase) ||
                                          e.Message.Contains("não encontrada", StringComparison.OrdinalIgnoreCase)))
                return NotFound(resultado.Errors);

            return BadRequest(resultado.Errors);
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = PerfilExclusao)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        var command = new ExcluirLicencaCommand(id);
        var resultado = await _mediator.Send(command, cancellationToken);

        if (resultado.IsFailed)
        {
            if (resultado.Errors.Any(e => e.Message.Contains("não encontrado", StringComparison.OrdinalIgnoreCase) ||
                                          e.Message.Contains("não encontrada", StringComparison.OrdinalIgnoreCase)))
                return NotFound(resultado.Errors);

            return BadRequest(resultado.Errors);
        }

        return NoContent();
    }
}