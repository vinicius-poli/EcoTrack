using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcoTrack.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediatR(mediatrConfiguration =>
        {
            mediatrConfiguration.LicenseKey = configuration["LuckyPenny:LicenseKey"];
            mediatrConfiguration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        return services;
    }
}
