namespace Cs2MarketApi.Infrastructure;

public class SteamOptions
{
    
    public string ApiKey { get; set; } = "";

    
    public int Currency { get; set; } = 1;
}

public class DMarketOptions
{
    
    public string GameId { get; set; } = "a8db";
}

public class PriceRefreshOptions
{
    public int TtlMinutes { get; set; } = 60;
    public int BatchSize { get; set; } = 20;
}
