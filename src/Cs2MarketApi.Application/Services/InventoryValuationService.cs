using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Application.Models;

namespace Cs2MarketApi.Application.Services;

public class InventoryValuationService(IInventoryProvider inventoryProvider, IPriceStore priceStore)
{
    public async Task<InventoryValuation> ValuateAsync(string steamId, int top = 10, CancellationToken ct = default)
    {
        var inventory = await inventoryProvider.GetInventoryAsync(steamId, ct);

        // only marketable items have a market price (cases, skins, stickers...)
        var groups = inventory
            .Where(i => i.Marketable && !string.IsNullOrWhiteSpace(i.MarketHashName))
            .GroupBy(i => i.MarketHashName)
            .ToList();

        var names = groups.Select(g => g.Key).ToList();
        var prices = await priceStore.GetPricesAsync(names, ct);

        var missing = names.Where(n => !prices.ContainsKey(n)).ToList();
        if (missing.Count > 0)
            await priceStore.RequestAsync(missing, ct);

        var items = new List<ValuedItem>();
        var totalBySource = new Dictionary<string, decimal>();

        foreach (var g in groups)
        {
            var first = g.First();
            var quantity = g.Sum(i => i.Amount);
            var itemPrices = prices.TryGetValue(g.Key, out var p) ? p : new Dictionary<string, decimal>();
            decimal? best = itemPrices.Count > 0 ? itemPrices.Values.Min() : null;

            foreach (var (source, price) in itemPrices)
                totalBySource[source] = totalBySource.GetValueOrDefault(source) + price * quantity;

            items.Add(new ValuedItem(
                g.Key, first.Name, first.IconUrl, quantity, itemPrices, best,
                (best ?? 0m) * quantity));
        }

        var ordered = items.OrderByDescending(i => i.TotalValue).ToList();

        return new InventoryValuation(
            steamId,
            TotalItems: inventory.Sum(i => i.Amount),
            PricedItems: items.Count(i => i.BestPrice.HasValue),
            PendingItems: items.Count(i => !i.BestPrice.HasValue),
            TotalValue: items.Sum(i => i.TotalValue),
            TotalBySource: totalBySource,
            TopItems: ordered.Take(top).ToList(),
            Items: ordered);
    }
}
