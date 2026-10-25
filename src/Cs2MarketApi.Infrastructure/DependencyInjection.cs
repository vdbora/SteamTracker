using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Infrastructure.Background;
using Cs2MarketApi.Infrastructure.Markets;
using Cs2MarketApi.Infrastructure.Persistence;
using Cs2MarketApi.Infrastructure.Steam;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cs2MarketApi.Infrastructure;

public static class DependencyInjection
{
    private const string UserAgent = "Mozilla/5.0 (compatible; Cs2MarketApi/1.0)";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SteamOptions>(config.GetSection("Steam"));
        services.Configure<DMarketOptions>(config.GetSection("DMarket"));
        services.Configure<PriceRefreshOptions>(config.GetSection("PriceRefresh"));

        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        // fixed version so the app can start even if MySQL is still booting
        services.AddDbContext<MarketDbContext>(o =>
            o.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

        services.AddHttpClient<ISteamProfileProvider, SteamProfileProvider>(c =>
        {
            c.BaseAddress = new Uri("https://api.steampowered.com/");
            c.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient<IInventoryProvider, SteamInventoryProvider>(c =>
        {
            c.BaseAddress = new Uri("https://steamcommunity.com/");
            c.Timeout = TimeSpan.FromSeconds(30);
            c.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });

        services.AddHttpClient<SteamMarketPriceProvider>(c =>
        {
            c.BaseAddress = new Uri("https://steamcommunity.com/");
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });

        services.AddHttpClient<DMarketPriceProvider>(c =>
        {
            c.BaseAddress = new Uri("https://api.dmarket.com/");
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });

        // every price source is registered as IPriceProvider
        services.AddTransient<IPriceProvider>(sp => sp.GetRequiredService<SteamMarketPriceProvider>());
        services.AddTransient<IPriceProvider>(sp => sp.GetRequiredService<DMarketPriceProvider>());

        services.AddScoped<IPriceStore, EfPriceStore>();
        services.AddHostedService<PriceRefreshWorker>();

        return services;
    }
}
