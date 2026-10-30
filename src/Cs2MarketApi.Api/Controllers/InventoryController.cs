using Cs2MarketApi.Application.Interfaces;
using Cs2MarketApi.Application.Models;
using Cs2MarketApi.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cs2MarketApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController(
    ISteamProfileProvider profiles,
    InventoryValuationService valuation) : ControllerBase
{
    /// <summary>
    /// CS2 inventory with prices and total value. Items whose price is not cached yet are reported as pending
    /// (pendingItems > 0) - call again after a while.
    /// </summary>
    [HttpGet("{idOrVanity}")]
    public async Task<ActionResult<InventoryValuation>> Get(
        string idOrVanity, [FromQuery] int top = 10, CancellationToken ct = default)
    {
        var steamId = await profiles.ResolveSteamIdAsync(idOrVanity, ct);
        return await valuation.ValuateAsync(steamId, Math.Clamp(top, 1, 100), ct);
    }
}
