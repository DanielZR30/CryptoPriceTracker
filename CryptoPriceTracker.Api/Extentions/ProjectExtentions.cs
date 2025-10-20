using CryptoPriceTracker.Infrastructure.Data;
using CryptoPriceTraker.Modules.Track;
using Microsoft.EntityFrameworkCore;
using CryptoPriceTraker.Core.Interfaces;
using CryptoPriceTracker.Infrastructure.Repositories;
using CryptoPriceTraker.Core.Proxies;
using CryptoPriceTracker.Infrastructure.Proxies;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace CryptoPriceTracker.Api.Extentions;

public static class ProjectExtentions
{
    public static IServiceCollection AddProjectExtentions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CoinGeckoSettings>(configuration.GetSection("CoinGecko"));
        services.AddControllersWithViews().AddNewtonsoftJson();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"))
                   .LogTo(
                       _ => {},
                       (eventId, logLevel) => false
                   );
        });
        services.AddHttpClient();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddScoped<ICryptoAssetRepository, CryptoAssetRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICoinGeckoProxy, CoinGeckoProxy>();

        services.AddTrackModuleDependencies();

        return services;
    }
}
