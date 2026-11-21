using Microsoft.FluentUI.AspNetCore.Components;

namespace Northpeak.Web.Components.Layout;

public partial class MainLayout
{
    private DesignThemeModes _mode = DesignThemeModes.System;

    private void ToggleTheme()
    {
        _mode = _mode == DesignThemeModes.Dark ? DesignThemeModes.Light : DesignThemeModes.Dark;
    }
}
