using System.Globalization;
using System.Text.Json;
using Cs2MarketApi.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Cs2MarketApi.Infrastructure.Markets;

//DMarket public market endpoint. Prices come in USD cents as strings
public class DMarketPriceProvider(HttpClient http, IOptions<DMarketOptions> options) : IPriceProvider
{
    public string Source => "dmarket";
    public TimeSpan RequestDelay => TimeSpan.FromMilliseconds(400);

    public async Task<decimal?> GetPriceAsync(string marketHashName, CancellationToken ct = default)
    {
        var url = $"exchange/v1/market/items?gameId={options.Value.GameId}"
                  + $"&title={Uri.EscapeDataString(marketHashName)}"
                  + "&currency=USD&limit=10&orderBy=price&orderDir=asc";

        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!doc.RootElement.TryGetProperty("objects", out var objects))
            return null;

        decimal? best = null;
        foreach (var o in objects.EnumerateArray())
        {
            // title search is fuzzy, keep only exact matches
            if (!string.Equals(o.Str("title"), marketHashName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!o.TryGetProperty("price", out var price))
                continue;

            if (decimal.TryParse(price.Str("USD"), NumberStyles.Number, CultureInfo.InvariantCulture, out var cents))
            {
                var usd = cents / 100m;
                if (best is null || usd < best) best = usd;
            }
        }

        return best;
    }
}
