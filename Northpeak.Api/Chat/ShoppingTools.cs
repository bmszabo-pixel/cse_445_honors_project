using System.Net.Mail;
using System.Text.Json;
using Google.GenAI.Types;
using Northpeak.Api.Catalog;
using Northpeak.Api.Orders;
using Northpeak.Api.Weather;
using Northpeak.Shared;
using SchemaType = Google.GenAI.Types.Type;

namespace Northpeak.Api.Chat;

public sealed class ShoppingTools(ProductCatalog catalog, OrderBook orders, OpenMeteoForecastProvider forecasts)
{
    public const string SaveTripDetails = "save_trip_details";
    public const string GetForecast = "get_forecast";
    public const string CheckStock = "check_stock";
    public const string GetOrderStatus = "get_order_status";
    public const string PlaceOrder = "place_order";

    private static readonly TimeSpan ReservationPeriod = TimeSpan.FromHours(48);

    private static readonly JsonSerializerOptions ResultJson = new(JsonSerializerDefaults.Web);

    public Tool Declarations { get; } = new()
    {
        FunctionDeclarations =
        [
            new FunctionDeclaration
            {
                Name = SaveTripDetails,
                Description = "Record trip details the shopper has stated or changed: destination, dates, budget, " +
                              "experience level, or gear preferences. Returns the saved trip and which details are still missing.",
                Parameters = Object(new()
                {
                    ["destination"] = Text("City or trail area, including the state, for example 'Flagstaff, AZ'."),
                    ["start_date"] = Text("Trip start date as YYYY-MM-DD."),
                    ["end_date"] = Text("Trip end date as YYYY-MM-DD."),
                    ["budget"] = new Schema { Type = SchemaType.Number, Description = "Total gear budget in US dollars." },
                    ["experience_level"] = Text("For example: first trip, some experience, experienced."),
                    ["preferences"] = Text("Gear preferences such as lightweight, solo, or already owned items.")
                })
            },
            new FunctionDeclaration
            {
                Name = GetForecast,
                Description = "Get the daily weather forecast for a destination and date range. " +
                              "If the dates are beyond the forecast range, the result includes a note to use seasonal conditions.",
                Parameters = Object(new()
                {
                    ["destination"] = Text("City or trail area, including the state."),
                    ["start_date"] = Text("YYYY-MM-DD"),
                    ["end_date"] = Text("YYYY-MM-DD")
                }, "destination", "start_date", "end_date")
            },
            new FunctionDeclaration
            {
                Name = CheckStock,
                Description = "Check live stock for a product by SKU (for example PAD-301) or by product name. " +
                              "Returns quantity in stock, price, and the restock date when out of stock.",
                Parameters = Object(new()
                {
                    ["sku_or_product_name"] = Text("A SKU or a product name.")
                }, "sku_or_product_name")
            },
            new FunctionDeclaration
            {
                Name = GetOrderStatus,
                Description = "Look up an order by ID (for example NP-10002). Returns status, carrier, tracking number, " +
                              "estimated delivery, and how the delivery date compares to the trip start date.",
                Parameters = Object(new()
                {
                    ["order_id"] = Text("The order ID, for example NP-10002.")
                }, "order_id")
            },
            new FunctionDeclaration
            {
                Name = PlaceOrder,
                Description = "Place an order and reserve the items for 48 hours. Call only after the shopper has confirmed " +
                              "the items, quantities, shipping speed, and total. A confirmation email with a payment link is sent to the shopper.",
                Parameters = Object(new()
                {
                    ["items"] = new Schema
                    {
                        Type = SchemaType.Array,
                        Description = "The products to order.",
                        Items = Object(new()
                        {
                            ["sku"] = Text("Product SKU, for example TEN-101."),
                            ["quantity"] = new Schema { Type = SchemaType.Integer, Description = "How many to order." }
                        }, "sku", "quantity")
                    },
                    ["shipping"] = new Schema
                    {
                        Type = SchemaType.String,
                        Description = "Shipping speed from the store policy.",
                        Enum = ["standard", "expedited", "overnight"]
                    },
                    ["customer_name"] = Text("The shopper's full name."),
                    ["email"] = Text("The shopper's email address for the confirmation and payment link.")
                }, "items", "shipping", "customer_name", "email")
            }
        ]
    };

    public async Task<Dictionary<string, object>> ExecuteAsync(
        string name, IReadOnlyDictionary<string, object>? rawArguments, ToolSession session, CancellationToken cancellationToken)
    {
        var arguments = new ToolArguments(rawArguments);
        object result = name switch
        {
            SaveTripDetails => SaveTrip(arguments, session),
            GetForecast => await ForecastAsync(arguments, session, cancellationToken),
            CheckStock => Stock(arguments, session),
            GetOrderStatus => OrderStatus(arguments, session),
            PlaceOrder => Order(arguments, session),
            _ => new { error = $"Unknown tool '{name}'." }
        };

        return JsonSerializer.SerializeToElement(result, ResultJson)
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => (object)property.Value.Clone());
    }

    private static object SaveTrip(ToolArguments arguments, ToolSession session)
    {
        var trip = session.Trip;
        session.Trip = trip with
        {
            Destination = arguments.GetString("destination") ?? trip.Destination,
            StartDate = arguments.GetDate("start_date") ?? trip.StartDate,
            EndDate = arguments.GetDate("end_date") ?? trip.EndDate,
            Budget = arguments.GetDecimal("budget") ?? trip.Budget,
            ExperienceLevel = arguments.GetString("experience_level") ?? trip.ExperienceLevel,
            Preferences = arguments.GetString("preferences") ?? trip.Preferences
        };

        var missing = session.Trip.MissingDetails();
        session.Record(SaveTripDetails, missing.Count == 0
            ? "Trip details complete"
            : $"Trip details saved; still need {string.Join(", ", missing)}");

        return new { trip = session.Trip, missingDetails = missing };
    }

    private async Task<object> ForecastAsync(ToolArguments arguments, ToolSession session, CancellationToken cancellationToken)
    {
        var destination = arguments.GetString("destination");
        var start = arguments.GetDate("start_date");
        var end = arguments.GetDate("end_date");
        if (destination is null || start is null || end is null)
        {
            return new { error = "destination, start_date and end_date (YYYY-MM-DD) are all required." };
        }

        var lookup = await forecasts.GetForecastAsync(destination, start.Value, end.Value, cancellationToken);
        if (lookup.Forecast is not { } forecast)
        {
            session.Record(GetForecast, $"Forecast unavailable for {destination}");
            return new { error = lookup.Error };
        }

        session.Forecast = forecast;
        session.Trip = session.Trip with
        {
            Destination = session.Trip.Destination ?? destination,
            StartDate = session.Trip.StartDate ?? start,
            EndDate = session.Trip.EndDate ?? end
        };

        session.Record(GetForecast, forecast.Days.Count == 0
            ? $"{forecast.Location}: beyond forecast range, using seasonal advice"
            : $"{forecast.Location}: lows near {forecast.LowestF:0}°F, highs near {forecast.HighestF:0}°F");

        return new
        {
            location = forecast.Location,
            lowestF = forecast.LowestF,
            highestF = forecast.HighestF,
            days = forecast.Days,
            note = forecast.Note
        };
    }

    private object Stock(ToolArguments arguments, ToolSession session)
    {
        var query = arguments.GetString("sku_or_product_name") ?? string.Empty;
        if (catalog.Find(query) is not { } product)
        {
            session.Record(CheckStock, $"No product matches '{query}'");
            return new { found = false, message = $"No catalog product matches '{query}'." };
        }

        session.Record(CheckStock, product.InStock
            ? $"{product.Name}: {product.Quantity} in stock"
            : $"{product.Name}: out of stock{(product.RestockDate is { } date ? $" until {date:MMM d}" : string.Empty)}");

        return new
        {
            found = true,
            product.Sku,
            product.Name,
            product.Price,
            product.InStock,
            product.Quantity,
            product.RestockDate
        };
    }

    private object OrderStatus(ToolArguments arguments, ToolSession session)
    {
        var orderId = arguments.GetString("order_id") ?? string.Empty;
        if (orders.Find(orderId) is not { } order)
        {
            session.Record(GetOrderStatus, $"Order {orderId} not found");
            return new { found = false, message = $"No order with ID '{orderId}' exists." };
        }

        var delivery = DeliveryAssessor.Assess(order, session.Trip.StartDate);
        session.AddOrder(new Shared.OrderStatus(order, delivery));
        session.Record(GetOrderStatus, $"{order.OrderId}: {order.Status}, {delivery.Verdict}");

        return new { found = true, order, delivery, tripStartDate = session.Trip.StartDate };
    }

    private object Order(ToolArguments arguments, ToolSession session)
    {
        var name = arguments.GetString("customer_name");
        var email = arguments.GetString("email");
        var speed = Shipping.Parse(arguments.GetString("shipping"));
        var requested = arguments.GetItems("items");

        var error = (name, email, speed, requested) switch
        {
            (null, _, _, _) => "The shopper's name is required.",
            (_, null, _, _) => "The shopper's email is required.",
            _ when !MailAddress.TryCreate(email, out _) => $"'{email}' is not a valid email address.",
            (_, _, null, _) => "Shipping must be standard, expedited, or overnight.",
            (_, _, _, { Count: 0 }) => "At least one item is required.",
            _ when requested.Any(item => item.Quantity < 1) => "Quantities must be at least 1.",
            _ => requested.Select(item => item.Sku).FirstOrDefault(sku => catalog.FindBySku(sku) is null) is { } unknown
                ? $"No product with SKU '{unknown}' exists."
                : null
        };

        var lines = error is null
            ? requested
                .GroupBy(item => item.Sku, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var product = catalog.FindBySku(group.Key)!;
                    return new OrderLine(product.Sku, product.Name, group.Sum(item => item.Quantity), product.Price);
                })
                .ToList()
            : [];

        error ??= catalog.Reserve(lines);
        if (error is not null)
        {
            session.Record(PlaceOrder, "Order not placed");
            return new { placed = false, error };
        }

        var subtotal = lines.Sum(line => line.LineTotal);
        var shippingCost = Shipping.Cost(speed!.Value, subtotal);
        var now = DateTimeOffset.Now;
        var order = orders.Add(id => new Shared.Order(
            id,
            name!,
            DateOnly.FromDateTime(now.DateTime),
            "Awaiting payment",
            $"{speed} shipping",
            null,
            Shipping.EstimateDelivery(speed.Value, now.DateTime),
            [.. lines, new OrderLine("SHIPPING", $"{speed} shipping", 1, shippingCost)],
            subtotal + shippingCost,
            now + ReservationPeriod));

        var delivery = DeliveryAssessor.Assess(order, session.Trip.StartDate);
        session.AddOrder(new Shared.OrderStatus(order, delivery));
        session.Record(PlaceOrder, $"Placed {order.OrderId}: {order.Total:C}, reserved for 48 hours");

        return new { placed = true, order, delivery, confirmationEmail = email };
    }

    private static Schema Text(string description) => new() { Type = SchemaType.String, Description = description };

    private static Schema Object(Dictionary<string, Schema> properties, params string[] required) => new()
    {
        Type = SchemaType.Object,
        Properties = properties,
        Required = required.Length == 0 ? null : [.. required]
    };
}
