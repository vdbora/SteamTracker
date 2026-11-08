using System.Globalization;
using System.Text;
using System.Text.Json;
using Cs2MarketApi.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSec.Cryptography;

namespace Cs2MarketApi.Infrastructure.Markets;

public class DMarketPriceProvider(
    HttpClient http,
    IOptions<DMarketOptions> options,
    ILogger<DMarketPriceProvider> logger) : IPriceProvider
{
    private const string Path = "/marketplace-api/v1/aggregated-prices";

    public string Source => "dmarket";
    public TimeSpan RequestDelay => TimeSpan.FromMilliseconds(500);

    public async Task<decimal?> GetPriceAsync(string marketHashName, CancellationToken ct = default)
    {
        var opt = options.Value;
        if (string.IsNullOrWhiteSpace(opt.PublicKey) || string.IsNullOrWhiteSpace(opt.SecretKey))
            throw new InvalidOperationException("DMarket:PublicKey / DMarket:SecretKey are not configured.");

        var body = JsonSerializer.Serialize(new
        {
            filter = new { game = opt.GameId, titles = new[] { marketHashName } },
            limit = "100"
        });

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var signature = Sign("POST" + Path + body + timestamp, opt.SecretKey);

        using var request = new HttpRequestMessage(HttpMethod.Post, Path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Api-Key", opt.PublicKey);
        request.Headers.Add("X-Sign-Date", timestamp);
        request.Headers.Add("X-Request-Sign", "dmar ed25519 " + signature);

        using var response = await http.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"DMarket returned {(int)response.StatusCode}: {Truncate(text)}", null, response.StatusCode);

               using var doc = JsonDocument.Parse(text);
        var items = Prop(doc.RootElement, "aggregatedPrices");

        if (items is { ValueKind: JsonValueKind.Array })
        {
            foreach (var item in items.Value.EnumerateArray())
            {
                if (!string.Equals(Prop(item, "title")?.GetString(), marketHashName, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Amount is in cents (USD): "26837" -> 268.37
                var offer = Prop(item, "offerBestPrice");
                var cents = offer is null ? null : ParseNumber(Prop(offer.Value, "Amount"));
                if (cents.HasValue)
                    return cents.Value / 100m;
            }
        }

        logger.LogWarning("DMarket: no price found for {Item}. Response: {Body}", marketHashName, Truncate(text));
        return null;
    }

    private static string Sign(string message, string secretKeyHex)
    {
        var secret = Convert.FromHexString(secretKeyHex.Trim());
        if (secret.Length < 32)
            throw new InvalidOperationException("DMarket secret key is too short.");

        var algorithm = SignatureAlgorithm.Ed25519;
        using var key = Key.Import(algorithm, secret.AsSpan(0, 32), KeyBlobFormat.RawPrivateKey);
        var signature = algorithm.Sign(key, Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(signature).ToLowerInvariant();
    }

    private static JsonElement? Prop(JsonElement el, string name)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in el.EnumerateObject())
            if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return p.Value;
        return null;
    }

    private static decimal? ParseNumber(JsonElement? el)
    {
        if (el is null) return null;
        if (el.Value.ValueKind == JsonValueKind.Number) return el.Value.GetDecimal();
        if (el.Value.ValueKind == JsonValueKind.String
            && decimal.TryParse(el.Value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
            return v;
        return null;
    }

    private static string Truncate(string s) => s.Length <= 500 ? s : s[..500] + "...";
}