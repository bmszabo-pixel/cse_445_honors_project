using Northpeak.Shared;

namespace Northpeak.Api.Orders;

public static class DeliveryAssessor
{
    public const int MinimumMarginDays = 2;

    public static DeliveryAssessment Assess(Order order, DateOnly? tripStart)
    {
        if (order.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
        {
            return new(DeliveryVerdict.Delivered, null, "This order has already been delivered.");
        }

        if (tripStart is null)
        {
            return new(DeliveryVerdict.Unknown, null,
                "No trip start date is known, so delivery timing was not compared.");
        }

        if (order.EstimatedDelivery is not { } eta)
        {
            return new(DeliveryVerdict.Unknown, null,
                "The carrier has not provided an estimated delivery date yet.");
        }

        var margin = tripStart.Value.DayNumber - eta.DayNumber;
        if (margin < 0)
        {
            return new(DeliveryVerdict.Late, margin,
                $"Estimated delivery {eta:MMM d} is {-margin} day(s) after the trip starts on {tripStart:MMM d}.");
        }

        if (margin < MinimumMarginDays)
        {
            return new(DeliveryVerdict.Tight, margin,
                $"Estimated delivery {eta:MMM d} leaves only {margin} day(s) before the trip starts on {tripStart:MMM d}.");
        }

        return new(DeliveryVerdict.OnTime, margin,
            $"Estimated delivery {eta:MMM d} leaves {margin} days before the trip starts on {tripStart:MMM d}.");
    }
}
