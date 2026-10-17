using Cs2MarketApi.Application.Models;

namespace Cs2MarketApi.Application.Interfaces;

public interface ISteamProfileProvider
{
    /// <summary>Accepts a 64-bit SteamID or a vanity name (custom URL) and returns the 64-bit SteamID.</summary>
    Task<string> ResolveSteamIdAsync(string idOrVanity, CancellationToken ct = default);

    Task<SteamProfile> GetProfileAsync(string steamId, CancellationToken ct = default);
}
