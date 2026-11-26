
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace EcoTrack.Application.Logging;
public static class SerilogLoggerFactory
{
    public static void AddSerilogLogger(
        this IServiceCollection services,
        ILoggingBuilder logging
    )
    {
        logging.ClearProviders();
        services.AddSerilog();
    }
}