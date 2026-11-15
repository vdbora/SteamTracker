using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Application.Models;
using Cs2MarketApi.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cs2MarketApi.Infrastructure.Persistence;

public class EfGameHistoryStore(MarketDbContext db) : IGameHistoryStore
{
    private const int ChunkSize = 500;

    public async Task<Dictionary<int, GameLowestPrice>> GetFreshAsync(
        IEnumerable<int> appIds, TimeSpan maxAge, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var result = new Dictionary<int, GameLowestPrice>();

        foreach (var chunk in appIds.Distinct().Chunk(ChunkSize))
        {
            var rows = await db.GameLows
                .AsNoTracking()
                .Where(r => chunk.Contains(r.AppId) && r.UpdatedAtUtc >= cutoff)
                .ToListAsync(ct);

            foreach (var r in rows)
            {
                DateTimeOffset? date = r.LowDateUtc is { } d
                    ? new DateTimeOffset(DateTime.SpecifyKind(d, DateTimeKind.Utc))
                    : null;
                result[r.AppId] = new GameLowestPrice(
                    r.AppId, r.Found, r.LowCents, r.RegularCents, r.CutPercent, date, r.Currency);
            }
        }

        return result;
    }

    public async Task SaveAsync(IEnumerable<GameLowestPrice> lows, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        foreach (var chunk in lows.Chunk(ChunkSize))
        {
            var ids = chunk.Select(l => l.AppId).ToList();
            var existing = await db.GameLows
                .Where(r => ids.Contains(r.AppId))
                .ToDictionaryAsync(r => r.AppId, ct);

            foreach (var l in chunk)
            {
                if (!existing.TryGetValue(l.AppId, out var row))
                {
                    row = new GameLowEntry { AppId = l.AppId };
                    db.GameLows.Add(row);
                }

                row.Found = l.Found;
                row.LowCents = l.LowCents;
                row.RegularCents = l.RegularCents;
                row.CutPercent = l.CutPercent;
                row.LowDateUtc = l.Date?.UtcDateTime;
                row.Currency = l.Currency;
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
