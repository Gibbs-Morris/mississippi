using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;

/// <summary>
///     Feature state for the Kitchen Sink MisFormField demo.
/// </summary>
internal sealed record MisFormFieldKitchenSinkState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misFormField";

    /// <summary>
    ///     Gets the current form field view model used by the demo component.
    /// </summary>
    public MisFormFieldViewModel ViewModel { get; init; } = MisFormFieldViewModel.Default with
    {
        Id = "demo-form-field",
        State = MisFormFieldState.Default,
    };

    /// <summary>
    ///     Gets the label text for the form field.
    /// </summary>
    public string LabelText { get; init; } = "Username";

    /// <summary>
    ///     Gets the help text for the form field.
    /// </summary>
    public string HelpText { get; init; } = "Enter your unique username.";

    /// <summary>
    ///     Gets the validation message for the form field.
    /// </summary>
    public string ValidationMessage { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the input value.
    /// </summary>
    public string InputValue { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether to show the validation message.
    /// </summary>
    public bool ShowValidation { get; init; }
}
