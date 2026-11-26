using EcoTrack.Application.Logging;
using EcoTrack.Application.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace EcoTrack.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration,
        ILoggingBuilder logging)
    {
        logging.ClearProviders();

        var serilogLogger = SerilogFactory.Create(configuration);
        Log.Logger = serilogLogger;
        services.AddSerilog(serilogLogger, dispose: true);

        services.AddMediatR(mediatrConfiguration =>
        {
            mediatrConfiguration.LicenseKey = configuration["LuckyPenny:LicenseKey"];
            mediatrConfiguration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        services.AddAutoMapper(mapperConfig =>
        {
            AutoMapperOptions autoMapperOptions = configuration
                .GetSection(AutoMapperOptions.SectionName)
                .Get<AutoMapperOptions>() ?? new AutoMapperOptions();

            string? licenseKey = autoMapperOptions.LicenseKey;

            if (!string.IsNullOrWhiteSpace(licenseKey))
                mapperConfig.LicenseKey = licenseKey;

            mapperConfig.AddMaps(typeof(DependencyInjection).Assembly);
        });

        return services;
    }
}