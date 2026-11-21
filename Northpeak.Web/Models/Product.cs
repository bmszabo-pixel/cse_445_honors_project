namespace Northpeak.Web.Models;

public record Product(
    string Id,
    string Name,
    string Category,
    decimal Price,
    int Stock,
    string Description
);
