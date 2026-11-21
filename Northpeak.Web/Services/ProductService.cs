using System.Net.Http.Json;
using Northpeak.Web.Models;

namespace Northpeak.Web.Services;

public class ProductService : IProductService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductService> _logger;

    public ProductService(HttpClient httpClient, ILogger<ProductService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var products = await _httpClient.GetFromJsonAsync<List<Product>>("api/products", cancellationToken);
            return products ?? (IReadOnlyList<Product>)Array.Empty<Product>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch products from backend API");
            throw;
        }
    }
}
