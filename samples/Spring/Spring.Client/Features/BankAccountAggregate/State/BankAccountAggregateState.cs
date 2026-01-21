using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Reservoir.Abstractions.State;


namespace Spring.Client.Features.BankAccountAggregate.State;

/// <summary>
///     Feature state for BankAccount aggregate command execution.
/// </summary>
/// <remarks>
///     This state tracks the status of command execution (loading, success, failure).
///     It does NOT hold read model data - that belongs in the projection feature state.
/// </remarks>
[PendingSourceGenerator]
internal sealed record BankAccountAggregateState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "bankAccountAggregate";

    /// <summary>
    ///     Gets the currently selected entity ID for command targeting.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    ///     Gets the error code from the last failed command.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    ///     Gets the error message from the last failed command.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets a value indicating whether a command is currently executing.
    /// </summary>
    public bool IsExecuting { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the last command succeeded.
    /// </summary>
    public bool? LastCommandSucceeded { get; init; }
}