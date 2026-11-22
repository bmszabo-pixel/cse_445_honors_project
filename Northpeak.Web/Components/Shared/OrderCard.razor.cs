using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Northpeak.Shared;

namespace Northpeak.Web.Components.Shared;

public partial class OrderCard
{
    [Parameter, EditorRequired]
    public OrderStatus Status { get; set; } = default!;

    private BadgeColor StatusColor => Status.Order.Status.ToLowerInvariant() switch
    {
        "delivered" => BadgeColor.Success,
        "shipped" => BadgeColor.Brand,
        "delayed" => BadgeColor.Danger,
        "awaiting payment" => BadgeColor.Warning,
        _ => BadgeColor.Informative
    };

    private MessageBarIntent Intent => Status.Delivery.Verdict switch
    {
        DeliveryVerdict.Late => MessageBarIntent.Error,
        DeliveryVerdict.Tight => MessageBarIntent.Warning,
        _ => MessageBarIntent.Success
    };

    private string Title => Status.Delivery.Verdict switch
    {
        DeliveryVerdict.Late => "Arrives after your trip starts",
        DeliveryVerdict.Tight => "Cutting it close",
        DeliveryVerdict.Delivered => "Delivered",
        _ => "Arrives in time"
    };
}
