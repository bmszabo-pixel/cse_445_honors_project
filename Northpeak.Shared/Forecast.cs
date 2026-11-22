namespace Northpeak.Shared;

public sealed record ForecastDay(
    DateOnly Date,
    string Conditions,
    double HighF,
    double LowF,
    int? PrecipitationChance,
    double? WindMph);

public sealed record Forecast(
    string Location,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<ForecastDay> Days,
    string? Note)
{
    public double? LowestF => Days.Count == 0 ? null : Days.Min(d => d.LowF);
    public double? HighestF => Days.Count == 0 ? null : Days.Max(d => d.HighF);
}
