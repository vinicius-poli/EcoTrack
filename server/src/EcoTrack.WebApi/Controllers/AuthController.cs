using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EcoTrack.Application.Auth;
using EcoTrack.Domain.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoTrack.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AutenticarUsuarioCommand command, CancellationToken ct)
    {
        var resultado = await mediator.Send(command, ct);

        if (resultado.IsFailed)
        {
            var ehFalhaAutenticacao = resultado.Errors.Any(e =>
                e.Metadata.TryGetValue("Campo", out var campo) && campo?.ToString() == "Autenticação");

            return ehFalhaAutenticacao
                ? Unauthorized(resultado.Errors)
                : BadRequest(resultado.Errors);
        }

        return Ok(resultado.Value);
    }

    [HttpPost("registrar")]
    [AllowAnonymous]
    public async Task<IActionResult> Registrar([FromBody] CadastrarUsuarioCommand command, CancellationToken ct)
    {
        var resultado = await mediator.Send(command, ct);

        if (resultado.IsFailed)
        {
            var ehConflitoEmail = resultado.Errors.Any(e =>
                e.Message.Contains("já está cadastrado", StringComparison.OrdinalIgnoreCase));

            return ehConflitoEmail
                ? Conflict(resultado.Errors)
                : BadRequest(resultado.Errors);
        }

        return CreatedAtAction(nameof(ObterUsuarioPorId), new { id = resultado.Value.Id }, resultado.Value);
    }

    [HttpPost("membros")]
    [Authorize(Roles = nameof(PerfilUsuario.Administrador))]
    public async Task<IActionResult> CadastrarMembro(
        [FromBody] CadastrarMembroRequest request,
        CancellationToken ct)
    {
        var command = new CadastrarMembroTenantCommand(
            request.Nome,
            request.Email,
            request.Senha,
            request.Perfil);

        var resultado = await mediator.Send(command, ct);

        if (resultado.IsFailed)
        {
            var ehConflitoEmail = resultado.Errors.Any(e =>
                e.Message.Contains("já está cadastrado", StringComparison.OrdinalIgnoreCase));

            return ehConflitoEmail
                ? Conflict(resultado.Errors)
                : BadRequest(resultado.Errors);
        }

        return CreatedAtAction(nameof(ObterUsuarioPorId), new { id = resultado.Value.Id }, resultado.Value);
    }

    [HttpGet("usuarios/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> ObterUsuarioPorId(Guid id, CancellationToken ct)
    {
        var resultado = await mediator.Send(new ObterUsuarioQuery(id), ct);

        if (resultado.IsFailed)
        {
            if (resultado.Errors.Any(e => e.Message.Contains("não encontrado", StringComparison.OrdinalIgnoreCase)))
                return NotFound(resultado.Errors);

            return BadRequest(resultado.Errors);
        }

        return Ok(resultado.Value);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> ObterUsuarioAtual(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(sub, out var usuarioId))
            return Unauthorized();

        var resultado = await mediator.Send(new ObterUsuarioQuery(usuarioId), ct);

        if (resultado.IsFailed)
        {
            if (resultado.Errors.Any(e => e.Message.Contains("não encontrado", StringComparison.OrdinalIgnoreCase)))
                return NotFound(resultado.Errors);

            return BadRequest(resultado.Errors);
        }

        return Ok(resultado.Value);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var expClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

        if (string.IsNullOrWhiteSpace(jti) || !long.TryParse(expClaim, out var expUnix))
            return BadRequest("Token inválido ou sem claims de sessão.");

        var expiraEmUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
        var resultado = await mediator.Send(new EncerrarSessaoCommand(jti, expiraEmUtc), ct);

        if (resultado.IsFailed)
            return BadRequest(resultado.Errors);

        return NoContent();
    }
}