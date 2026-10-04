using System.Collections.Generic;

using Microsoft.AspNetCore.Components;

using MississippiSamples.LightSpeed.Client.Features.Showcase;


namespace MississippiSamples.LightSpeed.Client.Components.Molecules.Profile;

/// <summary>Renders a profile form using supplied values and callbacks.</summary>
/// <remarks>Public so showcase pages can compose the form.</remarks>
public sealed partial class ProfileForm : ComponentBase
{
    private static IReadOnlyDictionary<string, object> EmailAttributes { get; } = new Dictionary<string, object>
    {
        ["name"] = "work-email",
        ["autocomplete"] = "email",
        ["required"] = true,
    };

    /// <summary>Gets or sets the email edit callback.</summary>
    [Parameter]
    public EventCallback<string> EmailChanged { get; set; }

    /// <summary>Gets or sets the reset callback.</summary>
    [Parameter]
    public EventCallback Reset { get; set; }

    /// <summary>Gets or sets the validation callback.</summary>
    [Parameter]
    public EventCallback Validate { get; set; }

    /// <summary>Gets or sets the selected presentation data.</summary>
    [Parameter]
    [EditorRequired]
    public ShowcaseView View { get; set; } = default!;
}