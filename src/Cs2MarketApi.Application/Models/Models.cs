namespace Cs2MarketApi.Application.Models;

public record SteamProfile(
    string SteamId,
    string PersonaName,
    string AvatarUrl,
    string ProfileUrl,
    bool IsPublic);

public record InventoryItem(
    string AssetId,
    string MarketHashName,
    string Name,
    string IconUrl,
    bool Marketable,
    int Amount);

public record ValuedItem(
    string MarketHashName,
    string Name,
    string IconUrl,
    int Quantity,
    Dictionary<string, decimal> Prices,   
    decimal? BestPrice,                   
    decimal TotalValue);                  

public record InventoryValuation(
    string SteamId,
    int TotalItems,
    int PricedItems,
    int PendingItems,                     
    decimal TotalValue,
    Dictionary<string, decimal> TotalBySource,
    IReadOnlyList<ValuedItem> TopItems,
    IReadOnlyList<ValuedItem> Items);
