namespace Northpeak.Api.Weather;

public static class WeatherCodes
{
    public static string Describe(int code) => code switch
    {
        0 => "Clear",
        1 or 2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 or 56 or 57 => "Drizzle",
        61 or 63 or 66 => "Rain",
        65 or 67 => "Heavy rain",
        71 or 73 or 77 => "Snow",
        75 => "Heavy snow",
        80 or 81 => "Rain showers",
        82 => "Heavy rain showers",
        85 or 86 => "Snow showers",
        95 => "Thunderstorms",
        96 or 99 => "Thunderstorms with hail",
        _ => "Mixed conditions"
    };
}
