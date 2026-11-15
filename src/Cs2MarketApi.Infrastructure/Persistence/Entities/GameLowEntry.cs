namespace Cs2MarketApi.Infrastructure.Persistence.Entities;

public class GameLowEntry
{
    public int AppId { get; set; }
    public bool Found { get; set; }
    public int LowCents { get; set; }
    public int RegularCents { get; set; }
    public int CutPercent { get; set; }
    public DateTime? LowDateUtc { get; set; }
    public string Currency { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
}
