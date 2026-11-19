namespace EcoTrack.Infrastructure.Persistence;

public sealed class TokenRevogado
{
    public Guid Id { get; set; }
    public string Jti { get; set; } = string.Empty;
    public DateTime ExpiracaoUtc { get; set; }
    public DateTime RevogadoEmUtc { get; set; }
}