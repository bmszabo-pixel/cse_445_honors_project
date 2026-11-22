using System.Globalization;
using System.Text.Json;
using Northpeak.Shared;

namespace Northpeak.Api.Weather;

public sealed class OpenMeteoForecastProvider(HttpClient httpClient, ILogger<OpenMeteoForecastProvider> logger)
{
    public const int ForecastHorizonDays = 16;

    private const string GeocodingUrl = "https://geocoding-api.open-meteo.com/v1/search";
    private const string ForecastUrl = "https://api.open-meteo.com/v1/forecast";

    public async Task<ForecastLookup> GetForecastAsync(
        string destination, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return ForecastLookup.Failed("A destination is required.");
        }

        if (end < start)
        {
            return ForecastLookup.Failed("The end date is before the start date.");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        if (end < today)
        {
            return ForecastLookup.Failed("The trip dates are in the past.");
        }

        try
        {
            var place = await GeocodeAsync(destination, cancellationToken);
            if (place is null)
            {
                return ForecastLookup.Failed($"No location matching '{destination}' was found.");
            }

            var lastForecastDay = today.AddDays(ForecastHorizonDays - 1);
            if (start > lastForecastDay)
            {
                return ForecastLookup.Success(new Forecast(place.Label, start, end, [],
                    $"The trip starts more than {ForecastHorizonDays} days from today, beyond the forecast range. " +
                    "Use typical seasonal conditions for this place and time of year instead."));
            }

            var from = start < today ? today : start;
            var to = end > lastForecastDay ? lastForecastDay : end;
            var days = await GetDailyForecastAsync(place, from, to, cancellationToken);
            var note = to < end
                ? $"Forecast is only available through {to:yyyy-MM-dd}; later days need seasonal estimates."
                : null;

            return ForecastLookup.Success(new Forecast(place.Label, start, end, days, note));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Forecast lookup failed for {Destination}", destination);
            return ForecastLookup.Failed("The weather service is unavailable right now.");
        }
    }

    private async Task<GeocodedPlace?> GeocodeAsync(string destination, CancellationToken cancellationToken)
    {
        var parts = destination.Split(',', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var name = parts[0];
        var region = parts.Length > 1 ? UsStates.Expand(parts[1]) : null;

        var url = $"{GeocodingUrl}?name={Uri.EscapeDataString(name)}&count=10&language=en&format=json";
        using var document = JsonDocument.Parse(await httpClient.GetStringAsync(url, cancellationToken));
        if (!document.RootElement.TryGetProperty("results", out var results))
        {
            return null;
        }

        var places = results.EnumerateArray().Select(GeocodedPlace.FromJson).ToList();
        var usPlaces = places.Where(p => p.CountryCode == "US").ToList();

        return (region is null ? null : places.FirstOrDefault(p => p.Matches(region)))
            ?? usPlaces.FirstOrDefault()
            ?? places.FirstOrDefault();
    }

    private async Task<IReadOnlyList<ForecastDay>> GetDailyForecastAsync(
        GeocodedPlace place, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        const string series = "weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,wind_speed_10m_max";
        var url = FormattableString.Invariant(
            $"{ForecastUrl}?latitude={place.Latitude}&longitude={place.Longitude}&daily={series}&temperature_unit=fahrenheit&wind_speed_unit=mph&timezone=auto&start_date={from:yyyy-MM-dd}&end_date={to:yyyy-MM-dd}");

        using var document = JsonDocument.Parse(await httpClient.GetStringAsync(url, cancellationToken));
        var daily = document.RootElement.GetProperty("daily");
        var dates = daily.GetProperty("time").EnumerateArray().ToList();

        return dates.Select((date, i) => new ForecastDay(
                Date: DateOnly.ParseExact(date.GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Conditions: WeatherCodes.Describe((int)(Value(daily, "weather_code", i) ?? -1)),
                HighF: Math.Round(Value(daily, "temperature_2m_max", i) ?? double.NaN),
                LowF: Math.Round(Value(daily, "temperature_2m_min", i) ?? double.NaN),
                PrecipitationChance: Value(daily, "precipitation_probability_max", i) is { } chance ? (int)chance : null,
                WindMph: Value(daily, "wind_speed_10m_max", i) is { } wind ? Math.Round(wind) : null))
            .Where(day => !double.IsNaN(day.HighF) && !double.IsNaN(day.LowF))
            .ToList();
    }

    private static double? Value(JsonElement daily, string series, int index) =>
        daily.TryGetProperty(series, out var values)
        && index < values.GetArrayLength()
        && values[index].ValueKind == JsonValueKind.Number
            ? values[index].GetDouble()
            : null;

    private sealed record GeocodedPlace(string Name, string? Admin1, string? Country, string? CountryCode, double Latitude, double Longitude)
    {
        public string Label => string.Join(", ", new[] { Name, Admin1, CountryCode == "US" ? null : Country }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        public bool Matches(string region) =>
            string.Equals(Admin1, region, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Country, region, StringComparison.OrdinalIgnoreCase)
            || string.Equals(CountryCode, region, StringComparison.OrdinalIgnoreCase);

        public static GeocodedPlace FromJson(JsonElement json) => new(
            json.GetProperty("name").GetString() ?? string.Empty,
            json.TryGetProperty("admin1", out var admin1) ? admin1.GetString() : null,
            json.TryGetProperty("country", out var country) ? country.GetString() : null,
            json.TryGetProperty("country_code", out var code) ? code.GetString() : null,
            json.GetProperty("latitude").GetDouble(),
            json.GetProperty("longitude").GetDouble());
    }
}
