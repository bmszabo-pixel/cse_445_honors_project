using Northpeak.Shared;

namespace Northpeak.Api.Chat;

public sealed class ToolSession(TripContext trip)
{
    private readonly List<ToolActivity> _activity = [];
    private readonly List<OrderStatus> _orders = [];

    public TripContext Trip { get; set; } = trip;

    public IReadOnlyList<ToolActivity> Activity => _activity;

    public Forecast? Forecast { get; set; }

    public IReadOnlyList<OrderStatus> Orders => _orders;

    public void AddOrder(OrderStatus order)
    {
        _orders.RemoveAll(o => o.Order.OrderId == order.Order.OrderId);
        _orders.Add(order);
    }

    public void Record(string toolName, string summary) => _activity.Add(new ToolActivity(toolName, summary));
}
