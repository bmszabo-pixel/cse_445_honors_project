using Microsoft.AspNetCore.Components;
using Northpeak.Shared;
using Northpeak.Web.Models;
using Northpeak.Web.Services;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Northpeak.Web.Components.Pages;

public partial class Chat
{
    private static readonly IReadOnlyList<PromptSuggestion> Suggestions =
    [
        new("Plan a weekend trip", "I'm going backpacking in Flagstaff, AZ next weekend, budget $500, first time.",
            new Icons.Regular.Size24.Location()),
        new("Check stock", "Is the AirLite pad in stock?", new Icons.Regular.Size24.Box()),
        new("Track my order", "Where is order NP-10002?", new Icons.Regular.Size24.VehicleTruckProfile()),
        new("Returns", "Can I return worn boots?", new Icons.Regular.Size24.ArrowUndo())
    ];

    private readonly List<ConversationMessage> _messages = [];
    private TripContext _trip = TripContext.Empty;
    private bool _isWaiting;
    private string? _error;

    [Inject]
    private ChatService ChatService { get; set; } = default!;

    private async Task SendAsync(string text)
    {
        if (_isWaiting || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var request = new ChatRequest(text, _messages.Select(m => new ChatTurn(m.Role, m.Content)).ToList(), _trip);
        _messages.Add(ConversationMessage.FromUser(text));
        _error = null;
        _isWaiting = true;

        try
        {
            var response = await ChatService.SendAsync(request);
            _messages.Add(ConversationMessage.FromAssistant(response));
            _trip = response.TripContext;
        }
        catch (ApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _isWaiting = false;
        }
    }

    private void Reset()
    {
        _messages.Clear();
        _trip = TripContext.Empty;
        _error = null;
    }
}
