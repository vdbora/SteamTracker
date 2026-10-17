namespace Cs2MarketApi.Application.Interfaces;

public interface IPriceProvider
{
    /// <summary>Name of the price source, e.g. "steam" or "dmarket".</summary>
    string Source { get; }

    /// <summary>Minimum pause between two requests (rate limiting).</summary>
    TimeSpan RequestDelay { get; }

    /// <summary>Lowest current price in USD, or null if the item is not listed.</summary>
    Task<decimal?> GetPriceAsync(string marketHashName, CancellationToken ct = default);
}

/// <summary>Cached prices (DB). The valuation service reads from here, a background worker fills it.</summary>
public interface IPriceStore
{
    /// <summary>marketHashName -> (source -> price). Only items that have at least one price are returned.</summary>
    Task<Dictionary<string, Dictionary<string, decimal>>> GetPricesAsync(
        IEnumerable<string> marketHashNames, CancellationToken ct = default);

    /// <summary>Registers items so the background worker starts fetching their prices.</summary>
    Task RequestAsync(IEnumerable<string> marketHashNames, CancellationToken ct = default);
}
