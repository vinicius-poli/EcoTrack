using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Condicionantes;
using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Domain.Licencas;
using EcoTrack.Infrastructure.Auth;
using EcoTrack.Infrastructure.Persistence;
using EcoTrack.Infrastructure.Persistence.Repositories;
using EcoTrack.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcoTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<EcoTrackDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddIdentityCore<EcoTrackUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<EcoTrackDbContext>();
        services
            .AddOptions<EvidenciaStorageOptions>()
            .BindConfiguration(EvidenciaStorageOptions.SectionName)
            .Validate(
                x => !string.IsNullOrWhiteSpace(x.BasePath),
                "A configuração EvidenciaStorage:BasePath é obrigatória."
            )
    .ValidateOnStart();

        services.AddScoped<IOrganizacaoRepository, OrganizacaoRepository>();
        services.AddScoped<IEmpreendimentoRepository, EmpreendimentoRepository>();
        services.AddScoped<ILicencaRepository, LicencaRepository>();
        services.AddScoped<ICondicionanteRepository, CondicionanteRepository>();
        services.AddScoped<IEvidenciaStorageService, LocalEvidenciaStorageService>();
        services.AddScoped<IGerenciadorIdentidade, GerenciadorIdentidade>();
        services.AddScoped<IEmissorTokens, JwtTokenGenerator>();
        services.AddScoped<IRevogacaoToken, RevogacaoTokenService>();

        return services;
    }
}