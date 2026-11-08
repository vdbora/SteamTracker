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
    
    /// Price of one item. Optional source = steam | dmarket: asks only that market live (no cache)
    /// and shows errors in the response.
   
    [HttpGet]
    public async Task<ActionResult<object>> Get(
        [FromQuery] string name, [FromQuery] string? source = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Query parameter 'name' (market_hash_name) is required.");

        var selected = providers
            .Where(p => source is null || p.Source.Equals(source, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (selected.Count == 0)
            return BadRequest("Unknown source. Use 'steam' or 'dmarket'.");

        // no source given -> use cache if we have it
        if (source is null)
        {
            var cached = await store.GetPricesAsync([name], ct);
            if (cached.TryGetValue(name, out var prices))
                return new { name, cached = true, prices };
        }

        var live = new Dictionary<string, decimal?>();
        var errors = new Dictionary<string, string>();

        foreach (var provider in selected)
        {
            try
            {
                live[provider.Source] = await provider.GetPriceAsync(name, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "{Source} price lookup failed for {Item}", provider.Source, name);
                errors[provider.Source] = ex.Message;
            }
        }

        if (source is null)
            await store.RequestAsync([name], ct);

        return new { name, cached = false, prices = live, errors };
    }
}