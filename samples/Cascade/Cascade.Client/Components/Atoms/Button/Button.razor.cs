using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;


namespace Cascade.Client.Components.Atoms.Button;

/// <summary>
///     Atomic component for interactive buttons with multiple style variants.
/// </summary>
public sealed partial class Button : ComponentBase
{
    /// <summary>
    ///     Gets or sets any additional HTML attributes to apply to the button.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the button content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the button is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the button should take full width.
    /// </summary>
    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>
    ///     Gets or sets the callback when the button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>
    ///     Gets or sets the button size. Valid values: md (default), lg, icon.
    /// </summary>
    [Parameter]
    public string Size { get; set; } = "md";

    /// <summary>
    ///     Gets or sets the button type. Valid values: button, submit, reset.
    /// </summary>
    [Parameter]
    public string Type { get; set; } = "button";

    /// <summary>
    ///     Gets or sets the button variant. Valid values: primary, secondary, ghost.
    /// </summary>
    [Parameter]
    public string Variant { get; set; } = "primary";

    private string SizeClass =>
        Size switch
        {
            "lg" => "btn--lg",
            "icon" => "btn--icon",
            var _ => string.Empty,
        };

    private string VariantClass =>
        Variant switch
        {
            "secondary" => "btn--secondary",
            "ghost" => "btn--ghost",
            var _ => "btn--primary",
        };

    private Task HandleClickAsync() => OnClick.InvokeAsync();
}