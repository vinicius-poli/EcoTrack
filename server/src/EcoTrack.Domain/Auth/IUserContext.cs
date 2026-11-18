namespace EcoTrack.Domain.Auth;

public interface IUserContext
{
    Guid? TenantId { get; }
    Guid? UserId { get; }
    string? Email { get; }
    string? Perfil { get; }
}