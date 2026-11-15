using System.Text.Json;
using Cs2MarketApi.Application.Exceptions;
using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Application.Models;
using Microsoft.Extensions.Options;

namespace Cs2MarketApi.Infrastructure.Steam;

public class SteamOwnedGamesProvider(HttpClient http, IOptions<SteamOptions> options) : IOwnedGamesProvider
{
    public async Task<IReadOnlyList<OwnedGame>> GetOwnedGamesAsync(string steamId, CancellationToken ct = default)
    {
        var url = $"IPlayerService/GetOwnedGames/v1/?key={options.Value.ApiKey}&steamid={steamId}"
                  + "&include_appinfo=1&include_played_free_games=1";

        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(url, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("Steam API is unreachable.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new ExternalServiceException($"Steam GetOwnedGames returned {(int)response.StatusCode}.");

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement.GetProperty("response");

            if (!root.TryGetProperty("games", out var games))
            {
                // {"response":{"game_count":0}} = really no games, {"response":{}} = private "Game details"
                if (root.TryGetProperty("game_count", out _))
                    return [];
                throw new GamesPrivateException(steamId);
            }

            var result = new List<OwnedGame>();
            foreach (var g in games.EnumerateArray())
            {
                var appId = g.Int("appid");
                var icon = g.Str("img_icon_url");
                result.Add(new OwnedGame(
                    appId,
                    g.Str("name") ?? $"App {appId}",
                    g.Int("playtime_forever"),
                    string.IsNullOrEmpty(icon)
                        ? ""
                        : $"https://media.steampowered.com/steamcommunity/public/images/apps/{appId}/{icon}.jpg"));
            }

            return result;
        }
    }
}
