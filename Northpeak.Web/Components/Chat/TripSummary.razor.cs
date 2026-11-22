using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Northpeak.Shared;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Northpeak.Web.Components.Chat;

public partial class TripSummary
{
    [Parameter, EditorRequired]
    public TripContext Trip { get; set; } = TripContext.Empty;

    private IReadOnlyList<(Icon Icon, string Text)> Items
    {
        get
        {
            var items = new List<(Icon, string)>();
            if (!string.IsNullOrWhiteSpace(Trip.Destination))
            {
                items.Add((new Icons.Regular.Size16.Location(), Trip.Destination));
            }

            if (Trip.StartDate is { } start)
            {
                var dates = Trip.EndDate is { } end && end != start ? $"{start:MMM d} – {end:MMM d}" : $"{start:MMM d}";
                items.Add((new Icons.Regular.Size16.CalendarLtr(), dates));
            }

            if (Trip.Budget is { } budget)
            {
                items.Add((new Icons.Regular.Size16.Money(), budget.ToString("C0")));
            }

            if (!string.IsNullOrWhiteSpace(Trip.ExperienceLevel))
            {
                items.Add((new Icons.Regular.Size16.PersonWalking(), Trip.ExperienceLevel));
            }

            return items;
        }
    }
}
