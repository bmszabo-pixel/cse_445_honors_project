using DotNetEnv;
using DotNetEnv.Configuration;
using Google.GenAI;
using Northpeak.Api.Catalog;
using Northpeak.Api.Chat;
using Northpeak.Api.Data;
using Northpeak.Api.Orders;
using Northpeak.Api.Weather;
using Northpeak.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDotNetEnv(Path.Combine(builder.Environment.ContentRootPath, ".env"), LoadOptions.NoClobber());
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.PostConfigure<GeminiOptions>(options => options.ApiKey ??= builder.Configuration["GEMINI_API_KEY"]);

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data");
var catalog = ProductCatalog.Load(Path.Combine(dataDirectory, "Products.csv"));
builder.Services.AddSingleton(catalog);
builder.Services.AddSingleton(OrderBook.Load(Path.Combine(dataDirectory, "Orders.csv"), catalog));
builder.Services.AddSingleton(KnowledgeBase.Load(
    Path.Combine(dataDirectory, "Northpeak_Product_Catalog.docx"),
    Path.Combine(dataDirectory, "Northpeak_Store_Policies.docx")));

builder.Services.AddHttpClient<OpenMeteoForecastProvider>(client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<ShoppingTools>();
builder.Services.AddSingleton<GeminiShoppingAssistant>();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

app.MapPost("/chat", async (ChatRequest request, GeminiShoppingAssistant assistant, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new ApiError("Message cannot be empty."));
    }

    if (!assistant.IsConfigured)
    {
        return Results.Json(new ApiError("The assistant is not configured. Add GEMINI_API_KEY to Northpeak.Api/.env."),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    try
    {
        return Results.Ok(await assistant.ReplyAsync(request, cancellationToken));
    }
    catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status429TooManyRequests)
    {
        logger.LogWarning(ex, "Gemini quota exceeded");
        return Results.Json(new ApiError("The Gemini API key has reached its usage limit. Try again later or use a key with a higher quota."),
            statusCode: StatusCodes.Status429TooManyRequests);
    }
    catch (ApiException ex)
    {
        logger.LogError(ex, "Gemini request failed");
        return Results.Json(new ApiError("The assistant is unavailable right now. Please try again in a moment."),
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.Run();
