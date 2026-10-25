using System.Net;
using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cs2MarketApi.Infrastructure.Background;

//Walks through stale / never-fetched rows and refreshes prices one by one 
public class PriceRefreshWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<PriceRefreshOptions> options,
    ILogger<PriceRefreshWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = 0;
            try
            {
                processed = await RefreshBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Price refresh batch failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(processed == 0 ? 30 : 1), stoppingToken);
        }
    }

    private async Task<int> RefreshBatchAsync(CancellationToken ct)
    {
        var opt = options.Value;
        var ttl = TimeSpan.FromMinutes(opt.TtlMinutes);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketDbContext>();
        var providers = scope.ServiceProvider.GetServices<IPriceProvider>().ToDictionary(p => p.Source);

        var cutoff = DateTime.UtcNow - ttl;
        var batch = await db.ItemPrices
            .Where(p => p.UpdatedAtUtc < cutoff)
            .OrderBy(p => p.UpdatedAtUtc)
            .Take(opt.BatchSize)
            .ToListAsync(ct);

        var processed = 0;
        foreach (var row in batch)
        {
            if (!providers.TryGetValue(row.Source, out var provider))
                continue;

            try
            {
                row.Price = await provider.GetPriceAsync(row.MarketHashName, ct);
                row.UpdatedAtUtc = DateTime.UtcNow;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                logger.LogWarning("{Source} rate limit hit, pausing for 2 minutes", provider.Source);
                await db.SaveChangesAsync(ct);
                await Task.Delay(TimeSpan.FromMinutes(2), ct);
                return processed;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to fetch {Source} price for {Item}", row.Source, row.MarketHashName);
                row.UpdatedAtUtc = DateTime.UtcNow - ttl + TimeSpan.FromMinutes(5); // retry in ~5 minutes
            }

            processed++;
            await Task.Delay(provider.RequestDelay, ct);
        }

        await db.SaveChangesAsync(ct);
        return processed;
    }
}
