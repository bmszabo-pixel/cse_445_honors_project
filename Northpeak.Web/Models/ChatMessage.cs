namespace Northpeak.Web.Models;

public record ChatMessage(
    string Id,
    string Content,
    bool IsUser,
    DateTime Timestamp,
    IReadOnlyList<ToolInvocation>? ToolCalls = null
);
