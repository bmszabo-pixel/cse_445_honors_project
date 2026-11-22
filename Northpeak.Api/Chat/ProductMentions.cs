using System.Text.RegularExpressions;
using Northpeak.Api.Catalog;
using Northpeak.Shared;

namespace Northpeak.Api.Chat;

public static partial class ProductMentions
{
    public static IReadOnlyList<Product> Find(string text, ProductCatalog catalog) =>
        SkuPattern().Matches(text)
            .Select(match => catalog.FindBySku(match.Value))
            .OfType<Product>()
            .DistinctBy(product => product.Sku)
            .ToList();

    [GeneratedRegex(@"\b[A-Z]{3}-\d{3}\b")]
    private static partial Regex SkuPattern();
}
