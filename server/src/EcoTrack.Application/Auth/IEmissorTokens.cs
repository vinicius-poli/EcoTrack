namespace EcoTrack.Application.Auth;

public interface IEmissorTokens
{
    string GerarToken(Guid usuarioId, string email, Guid tenantId);
}