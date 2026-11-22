using System.Globalization;
using System.Text.Json;

namespace Northpeak.Api.Chat;

public sealed class ToolArguments(IReadOnlyDictionary<string, object>? values)
{
    private readonly IReadOnlyDictionary<string, object> _values = values ?? new Dictionary<string, object>();

    public string? GetString(string name)
    {
        if (!_values.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        var text = value is JsonElement element
            ? element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText()
            : Convert.ToString(value, CultureInfo.InvariantCulture);

        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    public decimal? GetDecimal(string name)
    {
        var text = GetString(name)?.Replace("$", string.Empty).Replace(",", string.Empty);
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : null;
    }

    public IReadOnlyList<(string Sku, int Quantity)> GetItems(string name)
    {
        if (!_values.TryGetValue(name, out var value) || value is null)
        {
            return [];
        }

        var items = value as JsonElement? ?? JsonSerializer.SerializeToElement(value);
        if (items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return items.EnumerateArray()
            .Select(item => (
                Sku: item.TryGetProperty("sku", out var sku) ? sku.GetString()?.Trim() ?? string.Empty : string.Empty,
                Quantity: item.TryGetProperty("quantity", out var quantity) && quantity.ValueKind == JsonValueKind.Number
                    ? (int)quantity.GetDouble()
                    : 1))
            .ToList();
    }

    public DateOnly? GetDate(string name) =>
        DateOnly.TryParseExact(GetString(name), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
}
