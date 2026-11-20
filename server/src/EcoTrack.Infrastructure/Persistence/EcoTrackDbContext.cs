using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Condicionantes;
using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Domain.Licencas;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Infrastructure.Persistence;

public class EcoTrackDbContext : IdentityDbContext<EcoTrackUser, IdentityRole<Guid>, Guid>
{
    private readonly IUserContext _userContext;

    public EcoTrackDbContext(DbContextOptions<EcoTrackDbContext> options, IUserContext userContext)
        : base(options)
    {
        _userContext = userContext;
    }

    public DbSet<Empreendimento> Empreendimentos => Set<Empreendimento>();
    public DbSet<Licenca> Licencas => Set<Licenca>();
    public DbSet<Condicionante> Condicionantes => Set<Condicionante>();
    public DbSet<EvidenciaCondicionante> Evidencias => Set<EvidenciaCondicionante>();
    public DbSet<TokenRevogado> TokensRevogados => Set<TokenRevogado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EcoTrackDbContext).Assembly);

        modelBuilder.Entity<Empreendimento>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
        modelBuilder.Entity<Licenca>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
        modelBuilder.Entity<Condicionante>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
        modelBuilder.Entity<EvidenciaCondicionante>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
    }
}