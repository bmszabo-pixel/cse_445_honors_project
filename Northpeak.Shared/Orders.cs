using System.Text.Json.Serialization;

namespace Northpeak.Shared;

public sealed record OrderLine(string Sku, string Name, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record Order(
    string OrderId,
    string CustomerName,
    DateOnly OrderDate,
    string Status,
    string Carrier,
    string? TrackingNumber,
    DateOnly? EstimatedDelivery,
    IReadOnlyList<OrderLine> Items,
    decimal Total,
    DateTimeOffset? ReservedUntil = null);

[JsonConverter(typeof(JsonStringEnumConverter<DeliveryVerdict>))]
public enum DeliveryVerdict
{
    Unknown,
    OnTime,
    Tight,
    Late,
    Delivered
}

public sealed record DeliveryAssessment(DeliveryVerdict Verdict, int? MarginDays, string Message);

public sealed record OrderStatus(Order Order, DeliveryAssessment Delivery);
