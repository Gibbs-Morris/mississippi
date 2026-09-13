using Mississippi.Refraction.Client.Infrastructure.Theming;
using Mississippi.Reservoir.Abstractions.State;


namespace MississippiSamples.LightSpeed.Client.Features.Showcase;

/// <summary>Stores the local showcase preferences and form state.</summary>
internal sealed record ShowcaseState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "showcase";

    /// <summary>Gets the number of demonstrated actions.</summary>
    public int ActionCount { get; init; }

    /// <summary>Gets the editable email address.</summary>
    public string Email { get; init; } = "alex@contoso.example";

    /// <summary>Gets a value indicating whether validation was requested.</summary>
    public bool IsSubmitted { get; init; }

    /// <summary>Gets the latest demonstrated action.</summary>
    public string LastAction { get; init; } = "Ready";

    /// <summary>Gets the selected theme.</summary>
    public RefractionThemeMode ThemeMode { get; init; } = RefractionThemeMode.Dark;
}