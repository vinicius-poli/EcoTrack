using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EcoTrack.Domain.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EcoTrack.Infrastructure.Security;

public class JwtTokenGenerator : IEmissorTokens
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenGenerator(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public TokenDto CriarToken(CredenciaisUsuario usuario)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtOptions.SigningKey);
        var expiresInMinutes = _jwtOptions.ExpiresInMinutes > 0 ? _jwtOptions.ExpiresInMinutes : 60;
        var dataExpiracaoEmUtc = DateTime.UtcNow.AddMinutes(expiresInMinutes);
        var jti = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.Nome),
            new(JwtRegisteredClaimNames.Jti, jti),
            // Claims customizadas do EcoTrack para Multi-tenancy e Personas
            new("tenant_id", usuario.TenantId.ToString()),
            new(ClaimTypes.Role, usuario.Perfil)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = dataExpiracaoEmUtc,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        return new TokenDto(
            UserId: usuario.Id,
            TenantId: usuario.TenantId,
            Nome: usuario.Nome,
            Email: usuario.Email,
            Perfil: usuario.Perfil,
            AccessToken: accessToken,
            DataExpiracaoEmUtc: dataExpiracaoEmUtc
        );
    }
}