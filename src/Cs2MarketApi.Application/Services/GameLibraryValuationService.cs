using Cs2MarketApi.Application.Exceptions;
using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Application.Models;

namespace Cs2MarketApi.Application.Services;

public class GameLibraryValuationService(
    IOwnedGamesProvider ownedGames,
    IGamePriceProvider priceProvider,
    IGamePriceStore priceStore,
    IGameHistoryProvider historyProvider,
    IGameHistoryStore historyStore)
{
    private static readonly TimeSpan PriceMaxAge = TimeSpan.FromHours(12);
    private static readonly TimeSpan HistoryMaxAge = TimeSpan.FromDays(7); // historical lows rarely change

    public async Task<GameLibraryValuation> ValuateAsync(string steamId, int top = 10, CancellationToken ct = default)
    {
        var owned = (await ownedGames.GetOwnedGamesAsync(steamId, ct))
            .GroupBy(g => g.AppId).Select(g => g.First()).ToList();
        var ids = owned.Select(g => g.AppId).ToList();
        var warnings = new List<string>();

        // ---- current Steam Store prices ----
        var prices = await priceStore.GetFreshAsync(ids, PriceMaxAge, ct);
        var missingPrices = ids.Where(id => !prices.ContainsKey(id)).ToList();

        if (missingPrices.Count > 0)
        {
            try
            {
                var fetched = await priceProvider.GetPricesAsync(missingPrices, ct);
                if (fetched.Count > 0)
                {
                    await priceStore.SaveAsync(fetched.Values, ct);
                    foreach (var (id, price) in fetched)
                        prices[id] = price;
                }
            }
            catch (ExternalServiceException ex)
            {
                warnings.Add(ex.Message);
            }

            var stillMissing = ids.Count(id => !prices.ContainsKey(id));
            if (stillMissing > 0 && warnings.Count == 0)
                warnings.Add($"{stillMissing} game prices are not loaded yet (Steam rate limit). Call again in a few minutes.");
        }

        // ---- lowest Steam price ever (only for paid games) ----
        var paidIds = ids
            .Where(id => prices.TryGetValue(id, out var p) && p.Available && !p.IsFree)
            .ToList();

        var lows = await historyStore.GetFreshAsync(paidIds, HistoryMaxAge, ct);
        var missingLows = paidIds.Where(id => !lows.ContainsKey(id)).ToList();

        if (missingLows.Count > 0)
        {
            try
            {
                var fetched = await historyProvider.GetSteamLowsAsync(missingLows, ct);
                if (fetched.Count > 0)
                {
                    await historyStore.SaveAsync(fetched.Values, ct);
                    foreach (var (id, low) in fetched)
                        lows[id] = low;
                }
            }
            catch (ExternalServiceException ex)
            {
                warnings.Add(ex.Message);
            }
        }

        ValuedGame Build(OwnedGame g)
        {
            if (!prices.TryGetValue(g.AppId, out var p))
                return new ValuedGame(g.AppId, g.Name, g.PlaytimeMinutes, g.IconUrl,
                    null, null, null, null, null, false, "pending");
            if (!p.Available)
                return new ValuedGame(g.AppId, g.Name, g.PlaytimeMinutes, g.IconUrl,
                    null, null, null, null, null, false, "unavailable");
            if (p.IsFree)
                return new ValuedGame(g.AppId, g.Name, g.PlaytimeMinutes, g.IconUrl,
                    0m, 0m, null, null, null, true, "free");

            var full = p.InitialCents / 100m;
            var current = p.FinalCents / 100m;
            decimal? lowest = null;
            int? cut = null;
            DateTimeOffset? lowDate = null;

            if (lows.TryGetValue(g.AppId, out var low) && low.Found)
            {
                if (low.LowCents <= p.FinalCents)
                {
                    lowest = low.LowCents / 100m;
                    cut = low.CutPercent;
                    lowDate = low.Date;
                }
                else
                {
                    // history data lags behind: the current price is already the lowest one
                    lowest = current;
                    cut = full > 0 ? (int)Math.Round((1 - current / full) * 100) : 0;
                    lowDate = DateTimeOffset.UtcNow;
                }
            }

            return new ValuedGame(g.AppId, g.Name, g.PlaytimeMinutes, g.IconUrl,
                full, current, lowest, cut, lowDate, false, "priced");
        }

        var games = owned.Select(Build)
            .OrderByDescending(g => g.CurrentPrice ?? 0m)
            .ThenBy(g => g.Name)
            .ToList();

        var currency = prices.Values.Select(p => p.Currency).FirstOrDefault(c => !string.IsNullOrEmpty(c)) ?? "USD";

        return new GameLibraryValuation(
            steamId,
            currency,
            GamesCount: games.Count,
            PricedGames: games.Count(g => g.Status == "priced"),
            FreeGames: games.Count(g => g.Status == "free"),
            UnavailableGames: games.Count(g => g.Status == "unavailable"),
            PendingGames: games.Count(g => g.Status == "pending"),
            TotalFullPrice: games.Sum(g => g.FullPrice ?? 0m),
            TotalCurrentPrice: games.Sum(g => g.CurrentPrice ?? 0m),
            TotalLowestPrice: games.Where(g => g.Status == "priced").Sum(g => g.LowestPrice ?? g.CurrentPrice ?? 0m),
            Warning: warnings.Count == 0 ? null : string.Join(" ", warnings),
            TopGames: games.Where(g => g.CurrentPrice > 0).Take(top).ToList(),
            Games: games);
    }
}
