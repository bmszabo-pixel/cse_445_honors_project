using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Northpeak.Web.Models;
using Northpeak.Web.Services;

namespace Northpeak.Web.Components.Pages;

public partial class Orders
{
    private string _searchOrderId = "ORD-1001";
    private string _lastSearchedId = string.Empty;
    private Order? _order;
    private bool _isLoading;
    private bool _hasSearched;
    private string? _errorMessage;

    [Inject]
    private IOrderService OrderService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await SearchOrderAsync();
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !_isLoading && !string.IsNullOrWhiteSpace(_searchOrderId))
        {
            await SearchOrderAsync();
        }
    }

    private async Task QuickSelectOrder(string orderId)
    {
        _searchOrderId = orderId;
        await SearchOrderAsync();
    }

    private async Task SearchOrderAsync()
    {
        if (string.IsNullOrWhiteSpace(_searchOrderId))
        {
            return;
        }

        _isLoading = true;
        _errorMessage = null;
        _hasSearched = true;
        _lastSearchedId = _searchOrderId.Trim();

        try
        {
            _order = await OrderService.GetOrderByIdAsync(_lastSearchedId);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to look up order: {ex.Message}";
            _order = null;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
