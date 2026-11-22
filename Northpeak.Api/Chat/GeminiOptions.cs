namespace Northpeak.Api.Chat;

public sealed class GeminiOptions
{
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gemini-3.5-flash";
}
