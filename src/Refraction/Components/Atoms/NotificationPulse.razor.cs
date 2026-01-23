using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace Mississippi.Refraction.Components.Atoms;

/// <summary>
///     NotificationPulse component - a non-intrusive alert indicator.
/// </summary>
/// <remarks>
///     <para>
///         This component follows the state-down, events-up pattern. All data is received
///         via parameters and all user interactions are reported via EventCallbacks.
///         The component never mutates state internally.
///     </para>
///     <para>Anatomy:</para>
///     <list type="bullet">
///         <item>
///             <description>N0 Dot: small attention indicator</description>
///         </item>
///         <item>
///             <description>N1 Badge count: optional numeric badge</description>
///         </item>
///         <item>
///             <description>N2 Preview tick: optional one-line summary</description>
///         </item>
///     </list>
/// </remarks>
public partial class NotificationPulse : ComponentBase
{
    /// <summary>Gets or sets additional HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the child content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the callback when notification is dismissed.</summary>
    [Parameter]
    public EventCallback OnDismiss { get; set; }

    /// <summary>Gets or sets the callback when notification is clicked to expand.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnExpand { get; set; }

    /// <summary>Gets or sets the current component state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.New;

    /// <summary>Handles expand (click) event.</summary>
    private Task HandleClickAsync(
        MouseEventArgs e
    ) =>
        OnExpand.InvokeAsync(e);
}