using System.Collections.Generic;


namespace MississippiSamples.LightSpeed.Client.OperationsWorkbench;

/// <summary>
///     Represents the validation outcome for the review dialog draft.
/// </summary>
internal sealed record OperationsWorkbenchValidationResult
{
    /// <summary>
    ///     Gets the assigned-analyst validation error, when present.
    /// </summary>
    public string? AssignedAnalystError { get; init; }

    /// <summary>
    ///     Gets a value indicating whether any validation errors were produced.
    /// </summary>
    public bool HasErrors => Messages.Count > 0;

    /// <summary>
    ///     Gets the ordered validation messages shown in the summary block.
    /// </summary>
    public required IReadOnlyList<string> Messages { get; init; }

    /// <summary>
    ///     Gets the response-summary validation error, when present.
    /// </summary>
    public string? ResponseSummaryError { get; init; }

    /// <summary>
    ///     Gets the review-notes validation error, when present.
    /// </summary>
    public string? ReviewNotesError { get; init; }
}