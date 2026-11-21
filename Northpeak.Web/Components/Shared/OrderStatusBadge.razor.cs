using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Northpeak.Web.Components.Shared;

public partial class OrderStatusBadge
{
    [Parameter, EditorRequired]
    public string Status { get; set; } = string.Empty;

    private Appearance BadgeAppearance => Appearance.Accent;

    private string BadgeBackgroundColor => Status.ToLowerInvariant() switch
    {
        "delivered" => "#107c41",
        "shipped" => "#0078d4",
        "processing" => "#b25c00",
        _ => "#5c5c5c"
    };

    private string BadgeTextColor => "#ffffff";
}
