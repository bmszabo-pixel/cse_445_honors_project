using Northpeak.Shared;

namespace Northpeak.Api.Weather;

public sealed record ForecastLookup(Forecast? Forecast, string? Error)
{
    public static ForecastLookup Success(Forecast forecast) => new(forecast, null);

    public static ForecastLookup Failed(string error) => new(null, error);
}
