using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Empreendimentos;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Infrastructure.Persistence;

public class EcoTrackDbContext : DbContext
{
    private readonly IUserContext _userContext;

    public DbSet<Empreendimento> Empreendimentos => Set<Empreendimento>();

    public EcoTrackDbContext(DbContextOptions<EcoTrackDbContext> options, IUserContext userContext) : base(options)
    {
        _userContext = userContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica os mapeamentos Fluent API
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EcoTrackDbContext).Assembly);

        // Aplica o filtro global de Multi-tenancy automaticamente
        modelBuilder.Entity<Empreendimento>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);

        base.OnModelCreating(modelBuilder);
    }
}