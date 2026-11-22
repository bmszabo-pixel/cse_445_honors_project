using CsvHelper;
using Northpeak.Api.Data;
using Northpeak.Shared;

namespace Northpeak.Api.Catalog;

public sealed partial class ProductCatalog
{
    private static readonly HashSet<string> IgnoredWords =
        ["the", "a", "an", "is", "in", "stock", "do", "you", "have", "any", "of", "for", "my"];

    private readonly List<Product> _products;
    private readonly Dictionary<string, Product> _bySku;
    private readonly Lock _lock = new();

    public ProductCatalog(IEnumerable<Product> products)
    {
        _products = products.ToList();
        _bySku = _products.ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<Product> Products
    {
        get
        {
            lock (_lock)
            {
                return [.. _products];
            }
        }
    }

    public static ProductCatalog Load(string path) =>
        new(CsvFile.Read<ProductRow>(path).Select(row => new Product(
            row.Sku, row.Name, row.Category, row.Price, row.Quantity, row.RestockDate)));

    public Product? FindBySku(string sku)
    {
        lock (_lock)
        {
            return _bySku.GetValueOrDefault(sku.Trim());
        }
    }

    public string? Reserve(IReadOnlyList<OrderLine> lines)
    {
        lock (_lock)
        {
            foreach (var line in lines)
            {
                var product = _bySku[line.Sku];
                if (product.Quantity < line.Quantity)
                {
                    var restock = product.RestockDate is { } date ? $" Restock expected {date:MMM d}." : string.Empty;
                    var available = product.Quantity == 0 ? "is out of stock" : $"has only {product.Quantity} in stock";
                    return $"{product.Name} ({product.Sku}) {available}.{restock}";
                }
            }

            foreach (var line in lines)
            {
                var reserved = _bySku[line.Sku] with { Quantity = _bySku[line.Sku].Quantity - line.Quantity };
                _bySku[line.Sku] = reserved;
                _products[_products.FindIndex(p => p.Sku == line.Sku)] = reserved;
            }

            return null;
        }
    }

    public Product? Find(string skuOrName)
    {
        if (string.IsNullOrWhiteSpace(skuOrName))
        {
            return null;
        }

        if (FindBySku(skuOrName) is { } exact)
        {
            return exact;
        }

        var queryWords = Words(skuOrName).Where(w => !IgnoredWords.Contains(w)).ToList();
        if (queryWords.Count == 0)
        {
            return null;
        }

        return Products
            .Select(product => (product, score: Score(queryWords, Words(product.Name).ToHashSet())))
            .Where(match => match.score >= 0.5)
            .OrderByDescending(match => match.score)
            .Select(match => match.product)
            .FirstOrDefault();
    }

    private static double Score(IReadOnlyList<string> queryWords, HashSet<string> nameWords)
    {
        var matched = queryWords.Count(word =>
            nameWords.Contains(word) || nameWords.Contains(Singular(word)));

        return (double)matched / queryWords.Count;
    }

    private static string Singular(string word) =>
        word.Length > 3 && word.EndsWith('s') ? word[..^1] : word;

    private static IEnumerable<string> Words(string text) =>
        text.ToLowerInvariant()
            .Split([' ', '-', ',', '.', '?', '!', '(', ')', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Select(Singular);

    [CsvRecord]
    internal sealed partial record ProductRow(
        string Sku,
        string Name,
        string Category,
        decimal Price,
        int Quantity,
        DateOnly? RestockDate);
}
