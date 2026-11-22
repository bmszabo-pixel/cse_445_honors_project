using CsvHelper;
using System.Globalization;
using System.Text.RegularExpressions;
using Northpeak.Api.Catalog;
using Northpeak.Api.Data;
using Northpeak.Shared;

namespace Northpeak.Api.Orders;

public sealed partial class OrderBook
{
    private readonly Dictionary<string, Order> _orders;
    private readonly Lock _lock = new();

    public OrderBook(IEnumerable<Order> orders)
    {
        _orders = orders.ToDictionary(o => o.OrderId, StringComparer.OrdinalIgnoreCase);
    }

    public static OrderBook Load(string path, ProductCatalog catalog) =>
        new(CsvFile.Read<OrderRow>(path).Select(row => new Order(
            row.OrderId,
            row.CustomerName,
            row.OrderDate,
            row.Status,
            row.Carrier,
            string.IsNullOrWhiteSpace(row.TrackingNumber) ? null : row.TrackingNumber,
            row.EstimatedDelivery,
            ParseItems(row.Items, catalog),
            row.Total)));

    public Order? Find(string orderId)
    {
        lock (_lock)
        {
            return NormalizeOrderId(orderId) is { } id ? _orders.GetValueOrDefault(id) : null;
        }
    }

    public Order Add(Func<string, Order> create)
    {
        lock (_lock)
        {
            var next = _orders.Keys.Max(id => int.Parse(id[3..], CultureInfo.InvariantCulture)) + 1;
            var order = create($"NP-{next}");
            _orders[order.OrderId] = order;
            return order;
        }
    }

    private static string? NormalizeOrderId(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var match = OrderIdPattern().Match(input);
        return match.Success ? $"NP-{match.Groups["number"].Value}" : null;
    }

    private static IReadOnlyList<OrderLine> ParseItems(string items, ProductCatalog catalog) =>
        items.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item =>
            {
                var parts = item.Split(':');
                var product = catalog.FindBySku(parts[0])
                    ?? throw new FormatException($"Order item '{parts[0]}' is not in the catalog.");
                var quantity = parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 1;
                return new OrderLine(product.Sku, product.Name, quantity, product.Price);
            })
            .ToList();

    [GeneratedRegex(@"NP\s*-?\s*(?<number>\d{5})", RegexOptions.IgnoreCase)]
    private static partial Regex OrderIdPattern();

    [CsvRecord]
    internal sealed partial record OrderRow(
        string OrderId,
        string CustomerName,
        DateOnly OrderDate,
        string Status,
        string Carrier,
        string? TrackingNumber,
        DateOnly? EstimatedDelivery,
        string Items,
        decimal Total);
}
