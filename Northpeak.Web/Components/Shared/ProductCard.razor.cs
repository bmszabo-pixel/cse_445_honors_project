using Microsoft.AspNetCore.Components;
using Northpeak.Shared;

namespace Northpeak.Web.Components.Shared;

public partial class ProductCard
{
    [Parameter, EditorRequired]
    public Product Product { get; set; } = default!;

    private string StockText => Product switch
    {
        { InStock: true } => $"{Product.Quantity} in stock",
        { RestockDate: { } date } => $"Out of stock until {date:MMM d}",
        _ => "Out of stock"
    };
}
