using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EcoTrack.Domain.Auth;

namespace EcoTrack.WebApi.Services;

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? TenantId
    {
        get
        {
            var value = User?.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                        ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                     ?? User?.FindFirst(ClaimTypes.Email)?.Value;

    public string? Perfil => User?.FindFirst(ClaimTypes.Role)?.Value
                      ?? User?.FindFirst("role")?.Value;
}