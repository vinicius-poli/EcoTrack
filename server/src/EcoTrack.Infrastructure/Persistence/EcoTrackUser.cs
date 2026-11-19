using Microsoft.AspNetCore.Identity;

namespace EcoTrack.Infrastructure.Persistence;

public sealed class EcoTrackUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Perfil { get; set; } = "Usuario";
}