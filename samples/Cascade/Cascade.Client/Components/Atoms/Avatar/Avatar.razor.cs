using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Cascade.Client.Components.Atoms.Avatar;

/// <summary>
///     Atomic component for displaying user avatars with optional online status indicator.
/// </summary>
public sealed partial class Avatar : ComponentBase
{
    /// <summary>
    ///     Gets or sets any additional HTML attributes to apply to the root element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user is online.
    /// </summary>
    [Parameter]
    public bool IsOnline { get; set; }

    /// <summary>
    ///     Gets or sets the display name for generating initials.
    /// </summary>
    [Parameter]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether to show the online status indicator.
    /// </summary>
    [Parameter]
    public bool ShowStatus { get; set; }

    /// <summary>
    ///     Gets or sets the size of the avatar. Valid values: sm, md (default), lg, xl.
    /// </summary>
    [Parameter]
    public string Size { get; set; } = "md";

    private string AltText => $"Avatar for {Name}";

    private string Initials => GetInitials(Name);

    private string SizeClass =>
        Size switch
        {
            "sm" => "avatar--sm",
            "lg" => "avatar--lg",
            "xl" => "avatar--xl",
            var _ => string.Empty,
        };

    private string StatusClass => IsOnline ? "avatar__status--online" : string.Empty;

    private string StatusLabel => IsOnline ? "Online" : "Offline";

    private static string GetInitials(
        string name
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        string[] parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            var _ => $"{parts[0][..1]}{parts[^1][..1]}".ToUpperInvariant(),
        };
    }
}