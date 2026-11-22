using Northpeak.Shared;

namespace Northpeak.Web.Models;

public sealed record ConversationMessage(
    string Role,
    string Content,
    IReadOnlyList<ToolActivity> ToolActivity,
    IReadOnlyList<Product> Products,
    Forecast? Forecast = null,
    IReadOnlyList<OrderStatus>? Orders = null)
{
    public string Id { get; } = Guid.NewGuid().ToString("N");

    public bool IsUser => Role == ChatRoles.User;

    public static ConversationMessage FromUser(string text) => new(ChatRoles.User, text, [], []);

    public static ConversationMessage FromAssistant(ChatResponse response) =>
        new(ChatRoles.Assistant, response.Reply, response.ToolActivity, response.Products, response.Forecast, response.Orders);
}
