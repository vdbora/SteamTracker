namespace Cs2MarketApi.Application.Models;

public record OwnedGame(int AppId, string Name, int PlaytimeMinutes, string IconUrl);

/// Steam Store price of a game. Amounts are in cents of the store currency.
public record GamePrice(
    int AppId,
    string Currency,
    int InitialCents,   // base price
    int FinalCents,     // current price (with discount)
    bool IsFree,
    bool Available);    // false = Steam has no store data for this app (removed, region-locked...)

/// Lowest price a game ever had in the Steam store (data from IsThereAnyDeal).
public record GameLowestPrice(
    int AppId,
    bool Found,               // false = unknown to ITAD or no Steam history
    int LowCents,
    int RegularCents,         // base price at the time of the low
    int CutPercent,           // discount at the time of the low (e.g. 90)
    DateTimeOffset? Date,     // when the low happened
    string Currency);

/// priced | free | unavailable | pending
public record ValuedGame(
    int AppId,
    string Name,
    int PlaytimeMinutes,
    string IconUrl,
    decimal? FullPrice,
    decimal? CurrentPrice,
    decimal? LowestPrice,             // lowest Steam price ever (null = no history data)
    int? LowestDiscountPercent,       // discount at that moment
    DateTimeOffset? LowestPriceDate,
    bool IsFree,
    string Status);

public record GameLibraryValuation(
    string SteamId,
    string Currency,
    int GamesCount,
    int PricedGames,
    int FreeGames,
    int UnavailableGames,
    int PendingGames,                  // price not loaded yet (rate limit) - call again later
    decimal TotalFullPrice,            // sum of base prices
    decimal TotalCurrentPrice,         // sum of current store prices
    decimal TotalLowestPrice,          // library at the lowest Steam prices (current price if no history)
    string? Warning,
    IReadOnlyList<ValuedGame> TopGames,
    IReadOnlyList<ValuedGame> Games);
