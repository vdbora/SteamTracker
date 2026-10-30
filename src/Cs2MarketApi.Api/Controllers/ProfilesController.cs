using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cs2MarketApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfilesController(ISteamProfileProvider profiles) : ControllerBase
{
    /// <summary>Profile by 64-bit SteamID or vanity name, e.g. /api/profiles/gaben</summary>
    [HttpGet("{idOrVanity}")]
    public async Task<ActionResult<SteamProfile>> Get(string idOrVanity, CancellationToken ct)
    {
        var steamId = await profiles.ResolveSteamIdAsync(idOrVanity, ct);
        return await profiles.GetProfileAsync(steamId, ct);
    }
}
