namespace MississippiSamples.LightSpeed.Client.OperationsWorkbench;

/// <summary>
///     Describes a single work item rendered in the LightSpeed operations workbench.
/// </summary>
public sealed record OperationsWorkbenchItem
{
    /// <summary>
    ///     Gets the analyst currently assigned to the work item.
    /// </summary>
    public required string AssignedAnalyst { get; init; }

    /// <summary>
    ///     Gets the customer associated with the work item.
    /// </summary>
    public required string Customer { get; init; }

    /// <summary>
    ///     Gets the current disposition for the work item.
    /// </summary>
    public required string Disposition { get; init; }

    /// <summary>
    ///     Gets the stable work-item identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    ///     Gets the latest checkpoint message for the work item.
    /// </summary>
    public required string LastCheckpoint { get; init; }

    /// <summary>
    ///     Gets the queue name that owns the work item.
    /// </summary>
    public required string Queue { get; init; }

    /// <summary>
    ///     Gets the user-facing response summary.
    /// </summary>
    public required string ResponseSummary { get; init; }

    /// <summary>
    ///     Gets the review notes captured for the work item.
    /// </summary>
    public required string ReviewNotes { get; init; }

    /// <summary>
    ///     Gets the current stage shown in the workbench.
    /// </summary>
    public required string Stage { get; init; }
}