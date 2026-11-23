using EcoTrack.Application.Empreendimentos;
using EcoTrack.Domain.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoTrack.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmpreendimentosController : ControllerBase
{
    private const string PerfisEscrita =
        $"{nameof(PerfilUsuario.Gestor)},{nameof(PerfilUsuario.Administrador)}";

    private const string PerfilExclusao =
        nameof(PerfilUsuario.Administrador);

    private readonly IMediator _mediator;

    public EmpreendimentosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var query = new ListarEmpreendimentosQuery();
        var resultado = await _mediator.Send(query, cancellationToken);

        if (resultado.IsFailed)
            return BadRequest(resultado.Errors);

        return Ok(resultado.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var query = new ObterEmpreendimentoPorIdQuery(id);
        var resultado = await _mediator.Send(query, cancellationToken);

        if (resultado.IsFailed)
        {
            if (resultado.Errors.Any(e => e.Message.Contains("não encontrado", StringComparison.OrdinalIgnoreCase)))
                return NotFound(resultado.Errors);

            return BadRequest(resultado.Errors);
        }

        return Ok(resultado.Value);
    }

    [HttpPost]
    [Authorize(Roles = PerfisEscrita)]
    public async Task<IActionResult> Criar([FromBody] CriarEmpreendimentoRequest request, CancellationToken cancellationToken)
    {
        var command = new CriarEmpreendimentoCommand(request);
        var resultado = await _mediator.Send(command, cancellationToken);

        if (resultado.IsFailed)
            return BadRequest(resultado.Errors);

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Value }, resultado.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = PerfisEscrita)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarEmpreendimentoRequest request, CancellationToken cancellationToken)
    {
        var command = new AtualizarEmpreendimentoCommand(id, request);
        var resultado = await _mediator.Send(command, cancellationToken);

        if (resultado.IsFailed)
        {
            if (resultado.Errors.Any(e => e.Message.Contains("não encontrado", StringComparison.OrdinalIgnoreCase)))
                return NotFound(resultado.Errors);

            return BadRequest(resultado.Errors);
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = PerfilExclusao)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        var command = new ExcluirEmpreendimentoCommand(id);
        var resultado = await _mediator.Send(command, cancellationToken);

        if (resultado.IsFailed)
        {
            if (resultado.Errors.Any(e => e.Message.Contains("não encontrado", StringComparison.OrdinalIgnoreCase)))
                return NotFound(resultado.Errors);

            return BadRequest(resultado.Errors);
        }

        return NoContent();
    }
}