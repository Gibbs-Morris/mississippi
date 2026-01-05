using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;


namespace Cascade.Server.Components.Atoms;

/// <summary>
///     Atom component for the send message button.
/// </summary>
public sealed partial class SendButton : ComponentBase
{
    /// <summary>
    ///     Gets or sets a value indicating whether the button is disabled.
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>
    ///     Gets or sets the callback when button is clicked.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public EventCallback OnClick { get; set; }

    private async Task HandleClickAsync()
    {
        await OnClick.InvokeAsync();
    }
}