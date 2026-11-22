using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Northpeak.Shared;
using Northpeak.Web.Models;

namespace Northpeak.Web.Components.Chat;

public partial class ChatWindow : IAsyncDisposable
{
    private ElementReference _messagesElement;
    private IJSObjectReference? _module;
    private int _renderedCount = -1;
    private bool _renderedWaiting;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter, EditorRequired]
    public IReadOnlyList<ConversationMessage> Messages { get; set; } = [];

    [Parameter]
    public TripContext Trip { get; set; } = TripContext.Empty;

    [Parameter]
    public bool IsWaiting { get; set; }

    [Parameter]
    public string? Error { get; set; }

    [Parameter]
    public IReadOnlyList<PromptSuggestion> Suggestions { get; set; } = [];

    [Parameter]
    public EventCallback<string> OnSend { get; set; }

    [Parameter]
    public EventCallback OnReset { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Messages.Count == 0 || (Messages.Count == _renderedCount && IsWaiting == _renderedWaiting))
        {
            return;
        }

        _renderedCount = Messages.Count;
        _renderedWaiting = IsWaiting;

        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Chat/ChatWindow.razor.js");
            await _module.InvokeVoidAsync("scrollToBottom", _messagesElement);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
