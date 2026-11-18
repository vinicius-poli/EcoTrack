using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Empreendimentos;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Infrastructure.Persistence;

public class EcoTrackDbContext : DbContext
{
    private readonly IUserContext _userContext;

    public EcoTrackDbContext(DbContextOptions<EcoTrackDbContext> options, IUserContext userContext) 
        : base(options)
    {
        _userContext = userContext;
    }

    public DbSet<Empreendimento> Empreendimentos => Set<Empreendimento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica o Global Query Filter para isolar por TenantId automaticamente
        modelBuilder.Entity<Empreendimento>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);

        // Se houverem outras entidades com TenantId no futuro, você aplica aqui também:
        // modelBuilder.Entity<OutraEntidade>().HasQueryFilter(x => x.TenantId == _userContext.TenantId);
    }
}