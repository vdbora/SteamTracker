using System.Text.Json;
using Cs2MarketApi.Application.Exceptions;
using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Application.Models;
using Microsoft.Extensions.Options;

namespace Cs2MarketApi.Infrastructure.Steam;

public class SteamProfileProvider(HttpClient http, IOptions<SteamOptions> options) : ISteamProfileProvider
{
    private string Key => options.Value.ApiKey;

    public async Task<string> ResolveSteamIdAsync(string idOrVanity, CancellationToken ct = default)
    {
        idOrVanity = idOrVanity.Trim();

        if (idOrVanity.Length == 17 && idOrVanity.All(char.IsDigit))
            return idOrVanity;

        using var doc = await GetJsonAsync(
            $"ISteamUser/ResolveVanityURL/v1/?key={Key}&vanityurl={Uri.EscapeDataString(idOrVanity)}", ct);

        var response = doc.RootElement.GetProperty("response");
        if (response.Int("success") != 1)
            throw new SteamNotFoundException($"Steam user '{idOrVanity}' was not found.");

        return response.Str("steamid")!;
    }

    public async Task<SteamProfile> GetProfileAsync(string steamId, CancellationToken ct = default)
    {
        using var doc = await GetJsonAsync(
            $"ISteamUser/GetPlayerSummaries/v2/?key={Key}&steamids={steamId}", ct);

        var players = doc.RootElement.GetProperty("response").GetProperty("players");
        if (players.GetArrayLength() == 0)
            throw new SteamNotFoundException($"Steam user '{steamId}' was not found.");

        var p = players[0];
        return new SteamProfile(
            SteamId: p.Str("steamid") ?? steamId,
            PersonaName: p.Str("personaname") ?? "",
            AvatarUrl: p.Str("avatarfull") ?? "",
            ProfileUrl: p.Str("profileurl") ?? "",
            IsPublic: p.Int("communityvisibilitystate") == 3);
    }

    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                throw new ExternalServiceException($"Steam API returned {(int)response.StatusCode}.");

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("Steam API is unreachable.", ex);
        }
    }
}
