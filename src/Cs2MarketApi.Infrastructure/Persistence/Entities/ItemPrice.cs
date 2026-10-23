namespace Cs2MarketApi.Infrastructure.Persistence.Entities;

public class ItemPrice
{
    public int Id { get; set; }
    public string MarketHashName { get; set; } = "";
    public string Source { get; set; } = "";

    //if null then not fetched yet or not listed on the market
    public decimal? Price { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
