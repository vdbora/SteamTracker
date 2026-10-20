using System.Globalization;

namespace Cs2MarketApi.Infrastructure.Markets;

public static class PriceParser
{
    public static decimal? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var cleaned = new string(text.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray());
        if (cleaned.Length == 0) return null;

        var lastSep = cleaned.LastIndexOfAny([',', '.']);
        if (lastSep >= 0)
        {
            var integerPart = cleaned[..lastSep].Replace(",", "").Replace(".", "");
            var fraction = cleaned[(lastSep + 1)..];
            cleaned = $"{integerPart}.{fraction}";
        }

        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }
}
