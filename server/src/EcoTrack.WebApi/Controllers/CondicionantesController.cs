using EcoTrack.Application.Common;
using EcoTrack.Application.Condicionantes;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoTrack.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class CondicionantesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CondicionantesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // =========================================================================
    // RF03 - CONDICIONANTES
    // =========================================================================

    [HttpGet]
    [ProducesResponseType(typeof(List<CondicionanteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var resultado = await _mediator.Send(new ListarCondicionantesQuery(), cancellationToken);
        return resultado.IsSuccess ? Ok(resultado.Value) : BadRequest(resultado.Errors);
    }

    [HttpGet("por-licenca/{licencaId:guid}")]
    [ProducesResponseType(typeof(List<CondicionanteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPorLicenca(Guid licencaId, CancellationToken cancellationToken)
    {
        var resultado = await _mediator.Send(
            new ListarCondicionantesPorLicencaQuery(licencaId),
            cancellationToken);

        return resultado.IsSuccess ? Ok(resultado.Value) : BadRequest(resultado.Errors);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CondicionanteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _mediator.Send(new ObterCondicionantePorIdQuery(id), cancellationToken);
        if (resultado.IsSuccess)
            return Ok(resultado.Value);

        return NotFound(resultado.Errors);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(
        [FromBody] CriarCondicionanteRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _mediator.Send(new CriarCondicionanteCommand(request), cancellationToken);
        if (!resultado.IsSuccess)
            return BadRequest(resultado.Errors);

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = resultado.Value },
            resultado.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Atualizar(
        Guid id,
        [FromBody] AtualizarCondicionanteRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _mediator.Send(
            new AtualizarCondicionanteCommand(id, request),
            cancellationToken);

        return resultado.IsSuccess ? NoContent() : BadRequest(resultado.Errors);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _mediator.Send(new ExcluirCondicionanteCommand(id), cancellationToken);
        if (!resultado.IsSuccess)
            return BadRequest(resultado.Errors);

        // Se o handler adicionou aviso de remoção de arquivos via WithSuccess, devolvemos junto
        var avisos = resultado.Successes.Select(s => s.Message).ToList();
        return avisos.Count > 0
            ? Ok(new { mensagem = "Condicionante removida com sucesso.", avisos })
            : NoContent();
    }

    // =========================================================================
    // RF04 - EVIDÊNCIAS DE CONDICIONANTES
    // =========================================================================

    [HttpPost("{condicionanteId:guid}/evidencias")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AnexarEvidencia(
        Guid condicionanteId,
        [FromForm] AnexarEvidenciaFormRequest form,
        CancellationToken cancellationToken)
    {
        if (form.Arquivo is null)
        {
            return BadRequest(new[] { "O arquivo de evidência é obrigatório." });
        }

        await using var stream = form.Arquivo.OpenReadStream();

        var request = new AnexarEvidenciaRequest(
            CondicionanteId: condicionanteId,
            Conteudo: stream,
            NomeOriginalArquivo: form.Arquivo.FileName,
            ContentType: string.IsNullOrWhiteSpace(form.Arquivo.ContentType)
                ? "application/octet-stream"
                : form.Arquivo.ContentType,
            TamanhoBytes: form.Arquivo.Length,
            NumeroArt: form.NumeroArt,
            Observacao: form.Observacao
        );

        var resultado = await _mediator.Send(new AnexarEvidenciaCommand(request), cancellationToken);
        if (!resultado.IsSuccess)
            return BadRequest(resultado.Errors);

        return CreatedAtAction(
            nameof(BaixarEvidencia),
            new { condicionanteId, evidenciaId = resultado.Value },
            resultado.Value);
    }

    [HttpGet("{condicionanteId:guid}/evidencias/{evidenciaId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BaixarEvidencia(
        Guid condicionanteId,
        Guid evidenciaId,
        CancellationToken cancellationToken)
    {
        var resultado = await _mediator.Send(
            new BaixarEvidenciaQuery(condicionanteId, evidenciaId),
            cancellationToken);

        if (!resultado.IsSuccess)
            return NotFound(resultado.Errors);

        var arquivo = resultado.Value;
        return File(arquivo.Conteudo, arquivo.ContentType, arquivo.NomeOriginalArquivo);
    }

    [HttpDelete("{condicionanteId:guid}/evidencias/{evidenciaId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoverEvidencia(
        Guid condicionanteId,
        Guid evidenciaId,
        CancellationToken cancellationToken)
    {
        var resultado = await _mediator.Send(
            new RemoverEvidenciaCommand(condicionanteId, evidenciaId),
            cancellationToken);

        if (!resultado.IsSuccess)
            return BadRequest(resultado.Errors);

        var avisos = resultado.Successes.Select(s => s.Message).ToList();
        return avisos.Count > 0
            ? Ok(new { mensagem = "Evidência removida com sucesso.", avisos })
            : NoContent();
    }
}

public sealed class AnexarEvidenciaFormRequest
{
    public IFormFile? Arquivo { get; set; }
    public string? NumeroArt { get; set; }
    public string? Observacao { get; set; }
}