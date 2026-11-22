using Microsoft.AspNetCore.Components;

namespace Northpeak.Web.Components.Chat;

public partial class ErrorBar
{
    [Parameter]
    public string? Error { get; set; }
}
