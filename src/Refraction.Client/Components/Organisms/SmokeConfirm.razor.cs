using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Components.Organisms;

/// <summary>
///     SmokeConfirm component - a rare occlusion surface for destructive confirmation.
/// </summary>
/// <remarks>
///     <para>Anatomy:</para>
///     <list type="bullet">
///         <item>
///             <description>D0 Smoke veil: low opacity backdrop</description>
///         </item>
///         <item>
///             <description>D1 Confirm pane: small glass plane</description>
///         </item>
///         <item>
///             <description>D2 Action pair: confirm/cancel as reticle-selectable nodes</description>
///         </item>
///         <item>
///             <description>D3 Consequence line: one-line explanation only</description>
///         </item>
///     </list>
/// </remarks>
public partial class SmokeConfirm : ComponentBase
{
    /// <summary>Gets or sets additional HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the cancel button text.</summary>
    [Parameter]
    public string CancelText { get; set; } = "Cancel";

    /// <summary>Gets or sets the confirm button text.</summary>
    [Parameter]
    public string ConfirmText { get; set; } = "Confirm";

    /// <summary>Gets or sets the consequence description.</summary>
    [Parameter]
    public string? Consequence { get; set; }

    /// <summary>Gets or sets the callback when cancelled.</summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>Gets or sets the callback when confirmed.</summary>
    [Parameter]
    public EventCallback OnConfirm { get; set; }

    /// <summary>Gets or sets the current component state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Latent;

    /// <summary>Gets or sets the dialog title.</summary>
    [Parameter]
    public string? Title { get; set; }

    private string TitleId { get; } = $"rf-smoke-confirm-title-{Guid.NewGuid():N}";
}