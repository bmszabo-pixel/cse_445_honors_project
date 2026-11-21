using Microsoft.AspNetCore.Components;
using Northpeak.Web.Models;
using Northpeak.Web.Services;

namespace Northpeak.Web.Components.Pages;

public partial class Products
{
    private List<Product> _allProducts = new();
    private List<Product> _filteredProducts = new();
    private IQueryable<Product> _filteredQueryable = Enumerable.Empty<Product>().AsQueryable();
    private string _filterText = string.Empty;
    private bool _isLoading = true;
    private string? _errorMessage;

    [Inject]
    private IProductService ProductService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var result = await ProductService.GetProductsAsync();
            _allProducts = result.ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to connect to backend: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(_filterText))
        {
            _filteredProducts = _allProducts;
        }
        else
        {
            var query = _filterText.Trim();
            _filteredProducts = _allProducts.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        _filteredQueryable = _filteredProducts.AsQueryable();
    }
}
