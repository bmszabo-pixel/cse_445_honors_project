namespace Northpeak.Web.Models;

public record ToolInvocation(
    string ToolName,
    string ArgumentsSummary,
    string ResultSummary
);

public record ChatResponse(
    string SessionId,
    string Reply,
    IReadOnlyList<ToolInvocation>? ToolCalls,
    bool Success,
    string? Error
);
