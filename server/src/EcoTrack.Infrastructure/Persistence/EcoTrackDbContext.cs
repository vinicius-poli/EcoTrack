using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Empreendimentos;
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
    public DbSet<TokenRevogado> TokensRevogados => Set<TokenRevogado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EcoTrackDbContext).Assembly);

        modelBuilder.Entity<Empreendimento>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
    }
}