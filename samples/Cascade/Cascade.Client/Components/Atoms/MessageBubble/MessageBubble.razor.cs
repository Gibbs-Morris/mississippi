using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Cascade.Client.Components.Atoms.MessageBubble;

/// <summary>
///     Atomic component for displaying individual chat message content.
/// </summary>
public sealed partial class MessageBubble : ComponentBase
{
    /// <summary>
    ///     Gets or sets any additional HTML attributes to apply to the element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the message content text.
    /// </summary>
    [Parameter]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether the message has been deleted.
    /// </summary>
    [Parameter]
    public bool IsDeleted { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the message has been edited.
    /// </summary>
    [Parameter]
    public bool IsEdited { get; set; }
}