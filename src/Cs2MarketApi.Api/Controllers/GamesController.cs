using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Application.Models;
using Cs2MarketApi.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cs2MarketApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController(
    ISteamProfileProvider profiles,
    GameLibraryValuationService valuation) : ControllerBase
{
    /// <summary>
    /// Value of the user's game library by Steam Store prices (base and current).
    /// Requires "Game details" to be public in the user's Steam privacy settings.
    /// </summary>
    [HttpGet("{idOrVanity}")]
    public async Task<ActionResult<GameLibraryValuation>> Get(
        string idOrVanity, [FromQuery] int top = 10, CancellationToken ct = default)
    {
        var steamId = await profiles.ResolveSteamIdAsync(idOrVanity, ct);
        return await valuation.ValuateAsync(steamId, Math.Clamp(top, 1, 100), ct);
    }
}
