using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cs2MarketApi.Infrastructure.Persistence;

public class EfPriceStore(MarketDbContext db, IEnumerable<IPriceProvider> providers) : IPriceStore
{
    private const int ChunkSize = 500;

    public async Task<Dictionary<string, Dictionary<string, decimal>>> GetPricesAsync(
        IEnumerable<string> marketHashNames, CancellationToken ct = default)
    {
        var result = new Dictionary<string, Dictionary<string, decimal>>();

        foreach (var chunk in marketHashNames.Distinct().Chunk(ChunkSize))
        {
            var rows = await db.ItemPrices
                .AsNoTracking()
                .Where(p => chunk.Contains(p.MarketHashName) && p.Price != null)
                .ToListAsync(ct);

            foreach (var row in rows)
            {
                if (!result.TryGetValue(row.MarketHashName, out var bySource))
                    result[row.MarketHashName] = bySource = new Dictionary<string, decimal>();
                bySource[row.Source] = row.Price!.Value;
            }
        }

        return result;
    }

    public async Task RequestAsync(IEnumerable<string> marketHashNames, CancellationToken ct = default)
    {
        var sources = providers.Select(p => p.Source).ToList();
        var epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc); 

        foreach (var chunk in marketHashNames.Distinct().Chunk(ChunkSize))
        {
            var existing = (await db.ItemPrices
                    .Where(p => chunk.Contains(p.MarketHashName))
                    .Select(p => new { p.MarketHashName, p.Source })
                    .ToListAsync(ct))
                .Select(p => (p.MarketHashName, p.Source))
                .ToHashSet();

            foreach (var name in chunk)
            foreach (var source in sources)
                if (!existing.Contains((name, source)))
                    db.ItemPrices.Add(new ItemPrice { MarketHashName = name, Source = source, UpdatedAtUtc = epoch });
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // another request registered the same items at the same time - not a problem
        }
    }
}
