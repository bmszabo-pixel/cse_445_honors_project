namespace Northpeak.Api.Orders;

public enum ShippingSpeed
{
    Standard,
    Expedited,
    Overnight
}

public static class Shipping
{
    public static ShippingSpeed? Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "standard" => ShippingSpeed.Standard,
        "expedited" => ShippingSpeed.Expedited,
        "overnight" => ShippingSpeed.Overnight,
        _ => null
    };

    public static decimal Cost(ShippingSpeed speed, decimal subtotal) => speed switch
    {
        ShippingSpeed.Standard => subtotal >= 75m ? 0m : 7.95m,
        ShippingSpeed.Expedited => 14.95m,
        _ => 29.95m
    };

    public static DateOnly EstimateDelivery(ShippingSpeed speed, DateTime orderedAt)
    {
        var shipDate = DateOnly.FromDateTime(orderedAt);
        if (orderedAt.Hour >= 12 || !IsBusinessDay(shipDate))
        {
            shipDate = NextBusinessDay(shipDate);
        }

        var transitDays = speed switch
        {
            ShippingSpeed.Standard => 5,
            ShippingSpeed.Expedited => 2,
            _ => 1
        };

        for (var i = 0; i < transitDays; i++)
        {
            shipDate = NextBusinessDay(shipDate);
        }

        return shipDate;
    }

    private static DateOnly NextBusinessDay(DateOnly date)
    {
        do
        {
            date = date.AddDays(1);
        }
        while (!IsBusinessDay(date));

        return date;
    }

    private static bool IsBusinessDay(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}
