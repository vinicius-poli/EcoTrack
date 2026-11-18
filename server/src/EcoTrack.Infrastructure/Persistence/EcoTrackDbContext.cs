using EcoTrack.Domain.Empreendimentos;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Infrastructure.Persistence;

public class EcoTrackDbContext : DbContext
{
    public DbSet<Empreendimento> Empreendimentos => Set<Empreendimento>();

    public EcoTrackDbContext(DbContextOptions<EcoTrackDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EcoTrackDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}