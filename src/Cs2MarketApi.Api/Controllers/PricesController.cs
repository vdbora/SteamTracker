using Cs2MarketApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cs2MarketApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PricesController(
    IPriceStore store,
    IEnumerable<IPriceProvider> providers,
    ILogger<PricesController> logger) : ControllerBase
{
    /// <summary>Prices of one item from every source, e.g. /api/prices?name=AK-47 | Redline (Field-Tested)</summary>
    [HttpGet]
    public async Task<ActionResult<object>> Get([FromQuery] string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Query parameter 'name' (market_hash_name) is required.");

        var cached = await store.GetPricesAsync([name], ct);
        if (cached.TryGetValue(name, out var prices))
            return new { name, cached = true, prices };

        // not cached yet: ask the markets directly (used by the skin details popup) and queue it for the worker
        var live = new Dictionary<string, decimal>();
        foreach (var provider in providers)
        {
            try
            {
                var price = await provider.GetPriceAsync(name, ct);
                if (price.HasValue) live[provider.Source] = price.Value;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "{Source} price lookup failed for {Item}", provider.Source, name);
            }
        }

        await store.RequestAsync([name], ct);
        return new { name, cached = false, prices = live };
    }
}
