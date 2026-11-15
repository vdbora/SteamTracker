using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Application.Models;
using Cs2MarketApi.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cs2MarketApi.Infrastructure.Persistence;

public class EfGamePriceStore(MarketDbContext db) : IGamePriceStore
{
    private const int ChunkSize = 500;

    public async Task<Dictionary<int, GamePrice>> GetFreshAsync(
        IEnumerable<int> appIds, TimeSpan maxAge, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var result = new Dictionary<int, GamePrice>();

        foreach (var chunk in appIds.Distinct().Chunk(ChunkSize))
        {
            var rows = await db.GamePrices
                .AsNoTracking()
                .Where(r => chunk.Contains(r.AppId) && r.UpdatedAtUtc >= cutoff)
                .ToListAsync(ct);

            foreach (var r in rows)
                result[r.AppId] = new GamePrice(r.AppId, r.Currency, r.InitialCents, r.FinalCents, r.IsFree, r.Available);
        }

        return result;
    }

    public async Task SaveAsync(IEnumerable<GamePrice> prices, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        foreach (var chunk in prices.Chunk(ChunkSize))
        {
            var ids = chunk.Select(p => p.AppId).ToList();
            var existing = await db.GamePrices
                .Where(r => ids.Contains(r.AppId))
                .ToDictionaryAsync(r => r.AppId, ct);

            foreach (var p in chunk)
            {
                if (!existing.TryGetValue(p.AppId, out var row))
                {
                    row = new GamePriceEntry { AppId = p.AppId };
                    db.GamePrices.Add(row);
                }

                row.Currency = p.Currency;
                row.InitialCents = p.InitialCents;
                row.FinalCents = p.FinalCents;
                row.IsFree = p.IsFree;
                row.Available = p.Available;
                row.UpdatedAtUtc = now;
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // a parallel request saved the same games - not a problem
        }
    }
}
