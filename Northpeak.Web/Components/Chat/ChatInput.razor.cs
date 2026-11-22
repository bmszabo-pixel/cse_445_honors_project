using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Northpeak.Web.Components.Chat;

public partial class ChatInput
{
    private FluentTextInput? _input;
    private string? _text;
    private int _inputVersion;
    private bool _focusPending;

    [Parameter]
    public bool IsDisabled { get; set; }

    [Parameter]
    public EventCallback<string> OnSend { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusPending && !IsDisabled && _input is not null)
        {
            _focusPending = false;
            await _input.FocusAsync(false);
        }
    }

    private Task HandleEnterAsync(FluentKeyPressEventArgs args)
    {
        _text = args.Value;
        return SendAsync();
    }

    private async Task SendAsync()
    {
        var text = _text?.Trim();
        if (IsDisabled || string.IsNullOrEmpty(text))
        {
            return;
        }

        _text = string.Empty;
        _inputVersion++;
        _focusPending = true;
        await OnSend.InvokeAsync(text);
    }
}
