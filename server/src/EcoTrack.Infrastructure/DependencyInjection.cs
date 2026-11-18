using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Empreendimentos;
using EcoTrack.Infrastructure.Persistence;
using EcoTrack.Infrastructure.Persistence.Repositories;
using EcoTrack.Infrastructure.Security;
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

        services.AddScoped<IEmpreendimentoRepository, EmpreendimentoRepository>();

        services.AddScoped<IEmissorTokens, JwtTokenGenerator>();

        return services;
    }
}