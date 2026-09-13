using System;
using System.Collections.Generic;
using System.Globalization;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Client.Components.Atoms.Progress;

/// <summary>
///     Displays proportional completion or unknown-duration activity.
/// </summary>
/// <remarks>
///     Public so applications can compose this presentational atom. Supply an accessible name
///     through aria-label or aria-labelledby. Explicit state attributes take precedence over unmatched attributes.
/// </remarks>
public sealed partial class ProgressArc : ComponentBase
{
    /// <summary>Gets or sets additional HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets additional CSS classes.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Gets or sets a value indicating whether the theme scope disables animation.</summary>
    [CascadingParameter(Name = "RefractionReducedMotion")]
    public bool IsReducedMotion { get; set; }

    /// <summary>Gets or sets the finite maximum; the range must have a positive finite span.</summary>
    [Parameter]
    public double Max { get; set; } = 100;

    /// <summary>Gets or sets the finite minimum value.</summary>
    [Parameter]
    public double Min { get; set; }

    /// <summary>Gets or sets Determinate or Indeterminate from RefractionStates.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Determinate;

    /// <summary>Gets or sets finite completion, clamped to the range; ignored when indeterminate.</summary>
    [Parameter]
    public double Value { get; set; }

    private string? AccessibleValue => IsIndeterminate ? null : ClampedValue.ToString(CultureInfo.InvariantCulture);

    private double ClampedValue => Math.Clamp(Value, Min, Max);

    private string CssClass => string.IsNullOrWhiteSpace(Class) ? "rf-progress-arc" : $"rf-progress-arc {Class}";

    private string DashOffset =>
        (IsIndeterminate ? 75 : 100 - (((ClampedValue - Min) / (Max - Min)) * 100)).ToString(
            CultureInfo.InvariantCulture);

    private bool IsIndeterminate => State == RefractionStates.Indeterminate;

    private static void ValidateProgress(
        double min,
        double max,
        double value,
        string state
    )
    {
        if (!double.IsFinite(min))
        {
            throw new ArgumentOutOfRangeException(nameof(min), "Minimum must be finite.");
        }

        if (!double.IsFinite(max) || (max <= min) || !double.IsFinite(max - min))
        {
            throw new ArgumentOutOfRangeException(nameof(max), "Range must have a positive finite span.");
        }

        if (state is not (RefractionStates.Determinate or RefractionStates.Indeterminate))
        {
            throw new ArgumentOutOfRangeException(nameof(state), "Use Determinate or Indeterminate.");
        }

        if ((state == RefractionStates.Determinate) && !double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Determinate progress must be finite.");
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() => ValidateProgress(Min, Max, Value, State);
}