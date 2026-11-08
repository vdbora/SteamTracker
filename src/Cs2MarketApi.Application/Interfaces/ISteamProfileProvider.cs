using Cs2MarketApi.Application.Models;

namespace Cs2MarketApi.Application.Interfaces;

public interface ISteamProfileProvider
{
    //Accepts a SteamID or a vanity name\URL and returns the SteamID
    Task<string> ResolveSteamIdAsync(string idOrVanity, CancellationToken ct = default);

    Task<SteamProfile> GetProfileAsync(string steamId, CancellationToken ct = default);
}
