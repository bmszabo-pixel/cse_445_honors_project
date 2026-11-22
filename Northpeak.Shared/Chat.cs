namespace Northpeak.Shared;

public static class ChatRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";
}

public sealed record ChatTurn(string Role, string Content);

public sealed record ChatRequest(
    string Message,
    IReadOnlyList<ChatTurn>? History = null,
    TripContext? TripContext = null);

public sealed record ToolActivity(string Name, string Summary);

public sealed record ChatResponse(
    string Reply,
    TripContext TripContext,
    IReadOnlyList<ToolActivity> ToolActivity,
    IReadOnlyList<Product> Products,
    Forecast? Forecast = null,
    IReadOnlyList<OrderStatus>? Orders = null);
