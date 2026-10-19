# SteamTreacker

ASP.NET Core 8 Web API: Steam profile + CS2 inventory valuation.
Prices: Steam Community Market and DMarket.

## Run
1. `docker compose up -d`  (MySQL on :3306)
2. Put your Steam key in user-secrets:
   `dotnet user-secrets set "Steam:ApiKey" "YOUR_KEY" --project src/Cs2MarketApi.Api`
   (key: https://steamcommunity.com/dev/apikey)
3. `dotnet run --project src/Cs2MarketApi.Api`
4. Swagger: http://localhost:5000/swagger (see the port in the console)

## Endpoints
- `GET /api/profiles/{steamId | vanityName}`
- `GET /api/inventory/{steamId | vanityName}?top=10`
- `GET /api/prices?name=AK-47 | Redline (Field-Tested)`

## How prices work
Steam Market allows ~20 requests/min, so prices are cached in MySQL.
`InventoryController` returns what is cached and queues the rest (`pendingItems`);
`PriceRefreshWorker` fills the cache in the background and refreshes it every `PriceRefresh:TtlMinutes`.
Call the inventory endpoint again after a while to get the full value.

## Add another market
Implement `IPriceProvider` in `Infrastructure/Markets` and register it in `DependencyInjection.cs`.
