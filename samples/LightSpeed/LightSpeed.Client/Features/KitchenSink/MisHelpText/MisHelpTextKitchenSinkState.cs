using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisHelpText;

/// <summary>
///     Feature state for the Kitchen Sink MisHelpText demo.
/// </summary>
internal sealed record MisHelpTextKitchenSinkState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misHelpText";

    /// <summary>
    ///     Gets the current help text view model used by the demo component.
    /// </summary>
    public MisHelpTextViewModel ViewModel { get; init; } = MisHelpTextViewModel.Default with
    {
        Id = "demo-help-text",
    };

    /// <summary>
    ///     Gets the help text content displayed.
    /// </summary>
    public string HelpTextContent { get; init; } = "This is helpful instructional text for the form field.";
}
