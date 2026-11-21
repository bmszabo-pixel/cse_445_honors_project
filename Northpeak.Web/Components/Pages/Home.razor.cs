using Microsoft.AspNetCore.Components;

namespace Northpeak.Web.Components.Pages;

public partial class Home
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private void NavigateToChat()
    {
        Navigation.NavigateTo("chat");
    }

    private void NavigateToProducts()
    {
        Navigation.NavigateTo("products");
    }

    private void NavigateToOrders()
    {
        Navigation.NavigateTo("orders");
    }
}
