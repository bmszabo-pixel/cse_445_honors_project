using System.Net;
using System.Net.Http.Json;
using Northpeak.Web.Models;

namespace Northpeak.Web.Services;

public class OrderService : IOrderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderService> _logger;

    public OrderService(HttpClient httpClient, ILogger<OrderService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Order>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var orders = await _httpClient.GetFromJsonAsync<List<Order>>("api/orders", cancellationToken);
            return orders ?? (IReadOnlyList<Order>)Array.Empty<Order>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch orders from backend API");
            throw;
        }
    }

    public async Task<Order?> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return null;
        }

        try
        {
            var response = await _httpClient.GetAsync($"api/orders/{Uri.EscapeDataString(orderId.Trim())}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Order>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve order '{OrderId}'", orderId);
            throw;
        }
    }
}
