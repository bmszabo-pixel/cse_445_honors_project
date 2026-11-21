using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Northpeak.Web.Components.Chat;

public partial class ChatInput
{
    private string _inputText = string.Empty;

    [Parameter]
    public bool IsDisabled { get; set; }

    [Parameter]
    public EventCallback<string> OnSend { get; set; }

    private void HandleInputChanged()
    {
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey && !IsDisabled && !string.IsNullOrWhiteSpace(_inputText))
        {
            await SendAsync();
        }
    }

    private async Task SendAsync()
    {
        if (IsDisabled || string.IsNullOrWhiteSpace(_inputText))
        {
            return;
        }

        var text = _inputText.Trim();
        _inputText = string.Empty;
        await OnSend.InvokeAsync(text);
    }
}
