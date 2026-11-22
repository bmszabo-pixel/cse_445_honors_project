namespace Northpeak.Shared;

public sealed record TripContext(
    string? Destination = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    decimal? Budget = null,
    string? ExperienceLevel = null,
    string? Preferences = null)
{
    public static TripContext Empty { get; } = new();

    public IReadOnlyList<string> MissingDetails()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(Destination)) missing.Add("destination");
        if (StartDate is null || EndDate is null) missing.Add("trip dates");
        if (Budget is null) missing.Add("budget");
        if (string.IsNullOrWhiteSpace(ExperienceLevel)) missing.Add("experience level");
        return missing;
    }
}
