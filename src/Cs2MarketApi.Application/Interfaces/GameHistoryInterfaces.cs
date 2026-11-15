using Cs2MarketApi.Application.Models;

namespace Cs2MarketApi.Application.Interfaces;

public interface IGameHistoryProvider
{
   
    /// Lowest Steam store price ever for the given appids. May return fewer entries than requested
    /// if the provider rate-limits us - the rest will be loaded on the next call.
   
    Task<IReadOnlyDictionary<int, GameLowestPrice>> GetSteamLowsAsync(
        IEnumerable<int> appIds, CancellationToken ct = default);
}

public interface IGameHistoryStore
{
    Task<Dictionary<int, GameLowestPrice>> GetFreshAsync(
        IEnumerable<int> appIds, TimeSpan maxAge, CancellationToken ct = default);

    Task SaveAsync(IEnumerable<GameLowestPrice> lows, CancellationToken ct = default);
}
