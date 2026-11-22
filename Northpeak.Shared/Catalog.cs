namespace Northpeak.Shared;

public sealed record Product(
    string Sku,
    string Name,
    string Category,
    decimal Price,
    int Quantity,
    DateOnly? RestockDate)
{
    public bool InStock => Quantity > 0;
}
