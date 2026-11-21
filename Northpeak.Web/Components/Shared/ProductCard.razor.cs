using Microsoft.AspNetCore.Components;
using Northpeak.Web.Models;

namespace Northpeak.Web.Components.Shared;

public partial class ProductCard
{
    [Parameter, EditorRequired]
    public Product Product { get; set; } = default!;

    private bool IsInStock => Product.Stock > 0;

    private string StockBadgeBg => IsInStock ? "#107c41" : "#a80000";

    private string StockBadgeText => IsInStock ? $"{Product.Stock} in stock" : "Out of stock";
}
