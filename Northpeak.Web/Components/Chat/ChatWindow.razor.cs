using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Northpeak.Web.Models;

namespace Northpeak.Web.Components.Chat;

public partial class ChatWindow
{
    private ElementReference _messagesContainer;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter]
    public IReadOnlyList<Northpeak.Web.Models.ChatMessage> Messages { get; set; } = Array.Empty<Northpeak.Web.Models.ChatMessage>();

    [Parameter]
    public bool IsWaiting { get; set; }

    [Parameter]
    public EventCallback<string> OnSendMessage { get; set; }

    [Parameter]
    public EventCallback OnResetSession { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await ScrollToBottomAsync();
    }

    private async Task ScrollToBottomAsync()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("northpeakChat.scrollToBottom", _messagesContainer);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private async Task HandleSendMessageAsync(string text)
    {
        await OnSendMessage.InvokeAsync(text);
    }

    private async Task SendPromptAsync(string text)
    {
        if (!IsWaiting)
        {
            await OnSendMessage.InvokeAsync(text);
        }
    }

    private async Task ResetSessionAsync()
    {
        await OnResetSession.InvokeAsync();
    }
}
