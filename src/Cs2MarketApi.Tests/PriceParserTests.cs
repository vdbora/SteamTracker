using Cs2MarketApi.Infrastructure.Markets;

namespace Cs2MarketApi.Tests;

public class PriceParserTests
{
    [Theory]
    [InlineData("$1.23", 1.23)]
    [InlineData("$1,234.56", 1234.56)]
    [InlineData("1,23€", 1.23)]
    [InlineData("1.234,56€", 1234.56)]
    public void Parses_price_strings(string text, double expected)
        => Assert.Equal((decimal)expected, PriceParser.Parse(text));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("n/a")]
    public void Returns_null_for_garbage(string? text)
        => Assert.Null(PriceParser.Parse(text));
}
