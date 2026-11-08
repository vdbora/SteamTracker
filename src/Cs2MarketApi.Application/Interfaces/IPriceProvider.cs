namespace Cs2MarketApi.Application.Interfaces;

public interface IPriceProvider
{
    //Name of the price source, e.g. "steam" or "dmarket"
    string Source { get; }

    //Minimum pause between two requests (rate limiting)
    TimeSpan RequestDelay { get; }

    //Lowest current price in USD, or null if the item is not listed
    Task<decimal?> GetPriceAsync(string marketHashName, CancellationToken ct = default);
}

//The valuation service reads from here
public interface IPriceStore
{
    // Only items that have at least one price are returned
    Task<Dictionary<string, Dictionary<string, decimal>>> GetPricesAsync(
        IEnumerable<string> marketHashNames, CancellationToken ct = default);

    //Registers items so the background worker starts fetching their prices
    Task RequestAsync(IEnumerable<string> marketHashNames, CancellationToken ct = default);
}
