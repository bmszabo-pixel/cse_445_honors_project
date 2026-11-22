using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Northpeak.Web.Models;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Northpeak.Web.Components.Chat;

public partial class ChatMessage
{
    [Parameter, EditorRequired]
    public ConversationMessage Message { get; set; } = default!;

    private static Icon ToolIcon(string toolName) => toolName switch
    {
        "get_forecast" => new Icons.Regular.Size16.WeatherPartlyCloudyDay(),
        "check_stock" => new Icons.Regular.Size16.Box(),
        "get_order_status" => new Icons.Regular.Size16.VehicleTruckProfile(),
        _ => new Icons.Regular.Size16.Notepad()
    };
}
