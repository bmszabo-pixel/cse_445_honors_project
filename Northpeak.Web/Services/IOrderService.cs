using Northpeak.Web.Models;

namespace Northpeak.Web.Services;

public interface IOrderService
{
    Task<IReadOnlyList<Order>> GetOrdersAsync(CancellationToken cancellationToken = default);
    Task<Order?> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken = default);
}
