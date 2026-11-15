namespace Cs2MarketApi.Infrastructure.Persistence.Entities;

public class GamePriceEntry
{
    public int AppId { get; set; }
    public string Currency { get; set; } = "";
    public int InitialCents { get; set; }
    public int FinalCents { get; set; }
    public bool IsFree { get; set; }
    public bool Available { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
