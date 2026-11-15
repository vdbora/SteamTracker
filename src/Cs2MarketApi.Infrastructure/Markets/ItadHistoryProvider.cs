using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cs2MarketApi.Application.Exceptions;
using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Application.Models;
using Microsoft.Extensions.Options;

namespace Cs2MarketApi.Infrastructure.Markets;

/// <summary>
/// IsThereAnyDeal: lowest price a game ever had in the Steam store.
/// 1) appid -> ITAD game id  (POST /lookup/id/shop/61/v1)
/// 2) ITAD ids -> store lows (POST /games/storelow/v2?shops=61)
/// </summary>
public class ItadHistoryProvider(
    HttpClient http,
    IOptions<ItadOptions> itad,
    IOptions<SteamOptions> steam) : IGameHistoryProvider
{
    private const int SteamShopId = 61;
    private const int BatchSize = 50;

    public async Task<IReadOnlyDictionary<int, GameLowestPrice>> GetSteamLowsAsync(
        IEnumerable<int> appIds, CancellationToken ct = default)
    {
        var key = itad.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(key))
            throw new ExternalServiceException("Itad:ApiKey is not configured, lowest prices are skipped.");

        var country = steam.Value.StoreCountry.ToUpperInvariant();
        var result = new Dictionary<int, GameLowestPrice>();

        foreach (var chunk in appIds.Distinct().Chunk(BatchSize))
        {
            // 1) Steam appid -> ITAD game id
            using var lookup = await PostAsync(
                $"lookup/id/shop/{SteamShopId}/v1", chunk.Select(id => $"app/{id}").ToArray(), key, ct);
            if (lookup is null)
                break; // rate limit: keep what we have, the rest comes on the next call

            var byItadId = new Dictionary<string, int>();
            foreach (var entry in lookup.RootElement.EnumerateObject())
            {
                if (entry.Value.ValueKind == JsonValueKind.String
                    && int.TryParse(entry.Name.Replace("app/", ""), out var appId))
                {
                    byItadId[entry.Value.GetString()!] = appId;
                }
            }

            if (byItadId.Count > 0)
            {
                // 2) lowest price per store, only Steam
                using var lows = await PostAsync(
                    $"games/storelow/v2?country={country}&shops={SteamShopId}",
                    byItadId.Keys.ToArray(), key, ct);
                if (lows is null)
                    break;

                foreach (var item in lows.RootElement.EnumerateArray())
                {
                    var itadId = item.Str("id");
                    if (itadId is null || !byItadId.TryGetValue(itadId, out var appId))
                        continue;
                    if (!item.TryGetProperty("lows", out var lowList) || lowList.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var low in lowList.EnumerateArray())
                    {
                        if (!low.TryGetProperty("shop", out var shop) || shop.Int("id") != SteamShopId)
                            continue;
                        if (!low.TryGetProperty("price", out var price))
                            continue;

                        DateTimeOffset? date = DateTimeOffset.TryParse(
                            low.Str("timestamp"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                            ? parsed
                            : null;

                        var regular = low.TryGetProperty("regular", out var reg) ? reg.Int("amountInt") : 0;

                        result[appId] = new GameLowestPrice(
                            appId, true,
                            price.Int("amountInt"),
                            regular,
                            low.Int("cut"),
                            date,
                            price.Str("currency") ?? "");
                    }
                }
            }

            // games ITAD does not know (or without Steam history) are remembered as "not found"
            foreach (var appId in chunk)
                result.TryAdd(appId, new GameLowestPrice(appId, false, 0, 0, 0, null, ""));
        }

        return result;
    }

    private async Task<JsonDocument?> PostAsync(string url, string[] body, string key, CancellationToken ct)
    {
        // documented way of passing the key: query parameter "key"
        var separator = url.Contains('?') ? '&' : '?';
        var fullUrl = $"{url}{separator}key={Uri.EscapeDataString(key)}";

        using var request = new HttpRequestMessage(HttpMethod.Post, fullUrl) { Content = JsonContent.Create(body) };

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("IsThereAnyDeal is unreachable.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                var details = await response.Content.ReadAsStringAsync(ct);
                if (details.Length > 200) details = details[..200] + "...";
                throw new ExternalServiceException(
                    $"IsThereAnyDeal returned {(int)response.StatusCode}: {details}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
    }
}
