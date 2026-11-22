using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Northpeak.Shared;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Northpeak.Web.Components.Shared;

public partial class ForecastCard
{
    [Parameter, EditorRequired]
    public Forecast Forecast { get; set; } = default!;

    private static Icon WeatherIcon(string conditions) => conditions switch
    {
        "Clear" => new Icons.Regular.Size24.WeatherSunny(),
        "Overcast" or "Fog" => new Icons.Regular.Size24.WeatherCloudy(),
        _ when conditions.Contains("Snow") => new Icons.Regular.Size24.WeatherSnow(),
        _ when conditions.Contains("Thunder") => new Icons.Regular.Size24.WeatherThunderstorm(),
        _ when conditions.Contains("Rain") || conditions.Contains("Drizzle") => new Icons.Regular.Size24.WeatherRain(),
        _ => new Icons.Regular.Size24.WeatherPartlyCloudyDay()
    };
}
