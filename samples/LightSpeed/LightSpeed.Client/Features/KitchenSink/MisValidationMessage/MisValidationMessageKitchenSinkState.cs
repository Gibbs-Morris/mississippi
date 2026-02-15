using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisValidationMessage;

/// <summary>
///     Feature state for the Kitchen Sink MisValidationMessage demo.
/// </summary>
internal sealed record MisValidationMessageKitchenSinkState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misValidationMessage";

    /// <summary>
    ///     Gets the message text displayed within the validation message.
    /// </summary>
    public string MessageText { get; init; } = "This field is required.";

    /// <summary>
    ///     Gets the current validation message view model used by the demo component.
    /// </summary>
    public MisValidationMessageViewModel ViewModel { get; init; } = MisValidationMessageViewModel.Default with
    {
        For = "demo-input",
        Severity = MisValidationMessageSeverity.Error,
    };
}