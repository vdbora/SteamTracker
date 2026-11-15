using System.Text.Json;
using Cs2MarketApi.Application.Exceptions;
using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Application.Models;
using Microsoft.Extensions.Options;

namespace Cs2MarketApi.Infrastructure.Steam;

/// <summary>
/// Steam Store prices (undocumented public endpoint store.steampowered.com/api/appdetails).
/// With filters=price_overview it accepts a comma separated list of appids.
/// </summary>
public class SteamStorePriceProvider(HttpClient http, IOptions<SteamOptions> options) : IGamePriceProvider
{
    private const int BatchSize = 50;

    public async Task<IReadOnlyDictionary<int, GamePrice>> GetPricesAsync(
        IEnumerable<int> appIds, CancellationToken ct = default)
    {
        var result = new Dictionary<int, GamePrice>();
        var batches = appIds.Distinct().Chunk(BatchSize).ToList();

        for (var i = 0; i < batches.Count; i++)
        {
            if (i > 0)
                await Task.Delay(TimeSpan.FromSeconds(1.5), ct);

            var url = $"api/appdetails?appids={string.Join(",", batches[i])}"
                      + $"&filters=price_overview&cc={options.Value.StoreCountry}";

            HttpResponseMessage response;
            try
            {
                response = await http.GetAsync(url, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new ExternalServiceException("Steam Store is unreachable.", ex);
            }

            using (response)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    break; // keep what we have, the rest is loaded on the next call

                if (!response.IsSuccessStatusCode)
                    throw new ExternalServiceException(
                        $"Steam Store returned {(int)response.StatusCode} for a batch price request.");

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                foreach (var entry in doc.RootElement.EnumerateObject())
                {
                    if (!int.TryParse(entry.Name, out var appId))
                        continue;

                    result[appId] = ParseEntry(appId, entry.Value);
                }
            }
        }

        return result;
    }

    private static GamePrice ParseEntry(int appId, JsonElement entry)
    {
        var success = entry.ValueKind == JsonValueKind.Object
                      && entry.TryGetProperty("success", out var s)
                      && s.ValueKind == JsonValueKind.True;
        if (!success)
            return new GamePrice(appId, "", 0, 0, false, Available: false);

        if (entry.TryGetProperty("data", out var data)
            && data.ValueKind == JsonValueKind.Object
            && data.TryGetProperty("price_overview", out var po))
        {
            return new GamePrice(
                appId,
                po.Str("currency") ?? "",
                po.Int("initial"),
                po.Int("final"),
                IsFree: false,
                Available: true);
        }

        // free-to-play games come back with an empty "data": []
        return new GamePrice(appId, "", 0, 0, IsFree: true, Available: true);
    }
}
