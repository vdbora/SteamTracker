using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Application.Models;
using Cs2MarketApi.Application.Services;

namespace Cs2MarketApi.Tests;

public class InventoryValuationServiceTests
{
    private class FakeInventory(params InventoryItem[] items) : IInventoryProvider
    {
        public Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(string steamId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<InventoryItem>>(items);
    }

    private class FakeStore(Dictionary<string, Dictionary<string, decimal>> prices) : IPriceStore
    {
        public List<string> Requested { get; } = [];

        public Task<Dictionary<string, Dictionary<string, decimal>>> GetPricesAsync(
            IEnumerable<string> names, CancellationToken ct = default)
            => Task.FromResult(prices);

        public Task RequestAsync(IEnumerable<string> names, CancellationToken ct = default)
        {
            Requested.AddRange(names);
            return Task.CompletedTask;
        }
    }

    private static InventoryItem Item(string name, int amount = 1, bool marketable = true)
        => new("1", name, name, "", marketable, amount);

    [Fact]
    public async Task Total_uses_lowest_price_and_quantity()
    {
        var inventory = new FakeInventory(Item("Case", 3), Item("Knife"));
        var store = new FakeStore(new()
        {
            ["Case"] = new() { ["steam"] = 1.0m, ["dmarket"] = 0.8m },
            ["Knife"] = new() { ["steam"] = 100m }
        });

        var result = await new InventoryValuationService(inventory, store).ValuateAsync("123");

        Assert.Equal(3 * 0.8m + 100m, result.TotalValue);
        Assert.Equal("Knife", result.TopItems[0].MarketHashName);
        Assert.Equal(3m * 1.0m + 100m, result.TotalBySource["steam"]);
    }

    [Fact]
    public async Task Items_without_price_are_pending_and_requested()
    {
        var inventory = new FakeInventory(Item("Unknown skin"));
        var store = new FakeStore(new());

        var result = await new InventoryValuationService(inventory, store).ValuateAsync("123");

        Assert.Equal(1, result.PendingItems);
        Assert.Equal(0m, result.TotalValue);
        Assert.Contains("Unknown skin", store.Requested);
    }

    [Fact]
    public async Task Non_marketable_items_are_ignored()
    {
        var inventory = new FakeInventory(Item("Service medal", marketable: false));
        var store = new FakeStore(new());

        var result = await new InventoryValuationService(inventory, store).ValuateAsync("123");

        Assert.Empty(result.Items);
        Assert.Empty(store.Requested);
    }
}
