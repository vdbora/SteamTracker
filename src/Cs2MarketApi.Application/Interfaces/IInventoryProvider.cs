using Cs2MarketApi.Application.Models;

namespace Cs2MarketApi.Application.Interfaces;

public interface IInventoryProvider
{
    /// <summary>Returns the CS2 inventory (appid 730, context 2).</summary>
    Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(string steamId, CancellationToken ct = default);
}
