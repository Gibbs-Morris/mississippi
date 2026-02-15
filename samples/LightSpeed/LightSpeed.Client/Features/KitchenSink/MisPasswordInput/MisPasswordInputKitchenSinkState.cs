using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;

/// <summary>
///     Feature state for the Kitchen Sink MisPasswordInput demo.
/// </summary>
internal sealed record MisPasswordInputKitchenSinkState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misPasswordInput";

    /// <summary>
    ///     Gets the number of events recorded since the last clear.
    /// </summary>
    public int EventCount { get; init; }

    /// <summary>
    ///     Gets the list of emitted interaction event entries.
    /// </summary>
    public IReadOnlyList<string> EventLog { get; init; } = [];

    /// <summary>
    ///     Gets the current password input view model used by the demo component.
    /// </summary>
    public MisPasswordInputViewModel ViewModel { get; init; } = MisPasswordInputViewModel.Default with
    {
        IntentId = "kitchen-sink.mis-password-input",
        Placeholder = "Enter password",
        AriaLabel = "Password input",
    };
}