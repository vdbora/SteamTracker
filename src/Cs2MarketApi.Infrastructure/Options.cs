namespace Cs2MarketApi.Infrastructure;

public class SteamOptions
{
    /// Steam Web API key (https://steamcommunity.com/dev/apikey). Keep it in user-secrets / env vars.
    public string ApiKey { get; set; } = "";

    /// Steam currency id for Market prices. 1 = USD (same as DMarket), 3 = EUR.
    public int Currency { get; set; } = 1;

    /// Store country code for game prices. "us" = USD.
    public string StoreCountry { get; set; } = "us";
}

public class DMarketOptions
{
    /// DMarket game id for CS2 / CS:GO.
    public string GameId { get; set; } = "a8db";
    public string PublicKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
}

public class ItadOptions
{
    /// IsThereAnyDeal API key (https://isthereanydeal.com/apps/). Keep it in user-secrets / env vars.
    public string ApiKey { get; set; } = "";
}

public class PriceRefreshOptions
{
    public int TtlMinutes { get; set; } = 60;
    public int BatchSize { get; set; } = 20;
}
