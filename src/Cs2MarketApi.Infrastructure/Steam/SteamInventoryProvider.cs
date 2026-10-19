using System.Net;
using System.Text.Json;
using Cs2MarketApi.Application.Exceptions;
using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Application.Models;

namespace Cs2MarketApi.Infrastructure.Steam;

public class SteamInventoryProvider(HttpClient http) : IInventoryProvider
{
    private const string IconBase = "https://community.cloudflare.steamstatic.com/economy/image/";

    public async Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(string steamId, CancellationToken ct = default)
    {
        var result = new List<InventoryItem>();
        string? lastAssetId = null;
        var more = false;

        do
        {
            var url = $"inventory/{steamId}/730/2?l=english&count=2000"
                      + (lastAssetId is null ? "" : $"&start_assetid={lastAssetId}");

            HttpResponseMessage response;
            try
            {
                response = await http.GetAsync(url, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new ExternalServiceException("Steam inventory endpoint is unreachable.", ex);
            }

            using (response)
            {
                if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
                    throw new InventoryPrivateException(steamId);
                if (!response.IsSuccessStatusCode)
                    throw new ExternalServiceException($"Steam inventory returned {(int)response.StatusCode}.");

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    throw new InventoryPrivateException(steamId);

                if (!root.TryGetProperty("assets", out var assets))
                    break; // empty inventory

                
                var descriptions = new Dictionary<string, JsonElement>();
                if (root.TryGetProperty("descriptions", out var descs))
                    foreach (var d in descs.EnumerateArray())
                        descriptions[$"{d.Str("classid")}_{d.Str("instanceid")}"] = d;

                foreach (var a in assets.EnumerateArray())
                {
                    if (!descriptions.TryGetValue($"{a.Str("classid")}_{a.Str("instanceid")}", out var d))
                        continue;

                    int.TryParse(a.Str("amount"), out var amount);
                    var icon = d.Str("icon_url");

                    result.Add(new InventoryItem(
                        AssetId: a.Str("assetid") ?? "",
                        MarketHashName: d.Str("market_hash_name") ?? "",
                        Name: d.Str("name") ?? "",
                        IconUrl: icon is null ? "" : IconBase + icon,
                        Marketable: d.Int("marketable") == 1,
                        Amount: amount == 0 ? 1 : amount));
                }

                more = root.Int("more_items") == 1;
                lastAssetId = root.Str("last_assetid");
            }
        } while (more && lastAssetId is not null);

        return result;
    }
}
