using System.Text.Json;

namespace Cs2MarketApi.Infrastructure;

internal static class JsonHelpers
{
    public static string? Str(this JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object
        && el.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    public static int Int(this JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object
        && el.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32()
            : 0;
}
