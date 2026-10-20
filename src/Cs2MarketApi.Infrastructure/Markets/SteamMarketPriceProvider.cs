using System.Text.Json;
using Cs2MarketApi.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Cs2MarketApi.Infrastructure.Markets;

public class SteamMarketPriceProvider(HttpClient http, IOptions<SteamOptions> options) : IPriceProvider
{
    public string Source => "steam";
    public TimeSpan RequestDelay => TimeSpan.FromSeconds(4);

    public async Task<decimal?> GetPriceAsync(string marketHashName, CancellationToken ct = default)
    {
        var url = $"market/priceoverview/?appid=730&currency={options.Value.Currency}"
                  + $"&market_hash_name={Uri.EscapeDataString(marketHashName)}";

        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode(); // 429 -> HttpRequestException(StatusCode = TooManyRequests)

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        if (!root.TryGetProperty("success", out var ok) || ok.ValueKind != JsonValueKind.True)
            return null;

        return PriceParser.Parse(root.Str("lowest_price") ?? root.Str("median_price"));
    }
}
