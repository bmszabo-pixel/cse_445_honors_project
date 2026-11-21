using Microsoft.AspNetCore.Components;
using Northpeak.Web.Models;
using Northpeak.Web.Services;

namespace Northpeak.Web.Components.Pages;

public partial class Chat
{
    private readonly List<Northpeak.Web.Models.ChatMessage> _messages = new();
    private string _sessionId = Guid.NewGuid().ToString("N");
    private bool _isWaiting;
    private string? _errorMessage;

    [Inject]
    private IChatService ChatService { get; set; } = default!;

    protected override void OnInitialized()
    {
        InitializeWelcomeMessage();
    }

    private void InitializeWelcomeMessage()
    {
        _messages.Clear();
        _messages.Add(new Northpeak.Web.Models.ChatMessage(
            Id: Guid.NewGuid().ToString(),
            Content: "Hello! Welcome to **Northpeak Outfitters**. 🏔️\n\nI can help you with:\n- **Product Specs & Comparisons**: Weights, materials, and recommendations.\n- **Real-Time Stock Checks**: Ask if any outdoor item is currently in inventory.\n- **Order Tracking**: Enter an order ID like `ORD-1001` to check shipping status.\n- **Store Policies**: 30-day returns, free shipping over $50, and 1-year warranty.\n\nHow can I help you today?",
            IsUser: false,
            Timestamp: DateTime.UtcNow
        ));
    }

    private async Task SendMessageAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || _isWaiting)
        {
            return;
        }

        _errorMessage = null;

        var userMessage = new Northpeak.Web.Models.ChatMessage(
            Id: Guid.NewGuid().ToString(),
            Content: text,
            IsUser: true,
            Timestamp: DateTime.UtcNow
        );

        _messages.Add(userMessage);
        _isWaiting = true;

        try
        {
            var request = new ChatRequest(_sessionId, text);
            var response = await ChatService.SendMessageAsync(request);

            if (!response.Success && !string.IsNullOrEmpty(response.Error))
            {
                _errorMessage = $"Service warning: {response.Error}";
            }

            var assistantMessage = new Northpeak.Web.Models.ChatMessage(
                Id: Guid.NewGuid().ToString(),
                Content: response.Reply,
                IsUser: false,
                Timestamp: DateTime.UtcNow,
                ToolCalls: response.ToolCalls
            );

            _messages.Add(assistantMessage);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Unable to send message: {ex.Message}";
            _messages.Add(new Northpeak.Web.Models.ChatMessage(
                Id: Guid.NewGuid().ToString(),
                Content: "Sorry, I could not complete your request due to an error.",
                IsUser: false,
                Timestamp: DateTime.UtcNow
            ));
        }
        finally
        {
            _isWaiting = false;
        }
    }

    private void ResetSession()
    {
        _sessionId = Guid.NewGuid().ToString("N");
        _errorMessage = null;
        InitializeWelcomeMessage();
    }
}
