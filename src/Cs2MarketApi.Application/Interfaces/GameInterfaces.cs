using Cs2MarketApi.Application.Models;

namespace Cs2MarketApi.Application.Interfaces;

public interface IOwnedGamesProvider
{
    /// Games owned by the user. Throws GamesPrivateException if game details are private.
    Task<IReadOnlyList<OwnedGame>> GetOwnedGamesAsync(string steamId, CancellationToken ct = default);
}

public interface IGamePriceProvider
{
    
    /// Store prices for the given appids (Steam only). May return fewer entries than requested
    /// if Steam rate-limits us - the rest will be fetched on the next call.
    
    Task<IReadOnlyDictionary<int, GamePrice>> GetPricesAsync(IEnumerable<int> appIds, CancellationToken ct = default);
}

public interface IGamePriceStore
{
    Task<Dictionary<int, GamePrice>> GetFreshAsync(IEnumerable<int> appIds, TimeSpan maxAge, CancellationToken ct = default);
    Task SaveAsync(IEnumerable<GamePrice> prices, CancellationToken ct = default);
}
