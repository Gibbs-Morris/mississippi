using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisLabel;

/// <summary>
///     Feature state for the Kitchen Sink MisLabel demo.
/// </summary>
internal sealed record MisLabelKitchenSinkState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misLabel";

    /// <summary>
    ///     Gets the current label view model used by the demo component.
    /// </summary>
    public MisLabelViewModel ViewModel { get; init; } = MisLabelViewModel.Default with
    {
        For = "demo-input",
        IsRequired = false,
        State = MisLabelState.Default,
    };

    /// <summary>
    ///     Gets the content text displayed within the label.
    /// </summary>
    public string LabelText { get; init; } = "Demo Label";
}
