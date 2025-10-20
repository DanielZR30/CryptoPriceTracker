using CryptoPriceTraker.Modules.Track.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoPriceTraker.Modules.Track;

public static class Dependencies
{
    public static IServiceCollection AddTrackModuleDependencies(this IServiceCollection services)
    {
        services.AddScoped<ICryptoPriceService, CryptoPriceService>();
        return services;
    }
}
