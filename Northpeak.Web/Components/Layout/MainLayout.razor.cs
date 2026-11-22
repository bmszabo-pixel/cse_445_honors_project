using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Northpeak.Web.Components.Layout;

public partial class MainLayout
{
    [Inject]
    private IThemeService ThemeService { get; set; } = default!;

    private Task ToggleThemeAsync() => ThemeService.SwitchThemeAsync();
}
