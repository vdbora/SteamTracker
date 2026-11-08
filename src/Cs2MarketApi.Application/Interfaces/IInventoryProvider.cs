using Cs2MarketApi.Application.Models;

namespace Cs2MarketApi.Application.Interfaces;

public interface IInventoryProvider
{
    // Returns the CS2 inventory for the given Steam ID
    Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(string steamId, CancellationToken ct = default);
}
