using Cascade.Server.ViewModels;

using Microsoft.AspNetCore.Components;


namespace Cascade.Server.Components.Molecules;

/// <summary>
///     Molecule component for displaying a single message.
/// </summary>
public sealed partial class MessageItem : ComponentBase
{
    /// <summary>
    ///     Gets or sets a value indicating whether this message was sent by the current user.
    /// </summary>
    [Parameter]
    public bool IsOwn { get; set; }

    /// <summary>
    ///     Gets or sets the message to display. State flows DOWN via Parameter.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public MessageViewModel Message { get; set; } = default!;
}