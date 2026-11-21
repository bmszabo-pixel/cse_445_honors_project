namespace Northpeak.Web.Models;

public record OrderItem(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal Price
);

public record Order(
    string OrderId,
    string CustomerName,
    string OrderDate,
    string Status,
    string TrackingNumber,
    string Carrier,
    string Eta,
    IReadOnlyList<OrderItem> Items,
    decimal Total
);
