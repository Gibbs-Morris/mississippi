using System.Collections.Immutable;

using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Commands;
using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.State;
using Mississippi.Sdk.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.State;

/// <summary>
///     Feature state for BankAccount aggregate command execution.
/// </summary>
/// <remarks>
///     <para>
///         This state tracks the status of command execution (loading, success, failure)
///         with per-command history and correlation via <see cref="IAggregateCommandState" />.
///     </para>
///     <para>
///         It does NOT hold read model data - that belongs in the projection feature state.
///     </para>
/// </remarks>
[PendingSourceGenerator]
internal sealed record BankAccountAggregateState : IAggregateCommandState
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
    ///     Gets a value indicating whether any command is currently executing.
    /// </summary>
    public bool IsExecuting => !InFlightCommands.IsEmpty;

    /// <summary>
    ///     Gets a value indicating whether the last command succeeded.
    /// </summary>
    public bool? LastCommandSucceeded { get; init; }

    /// <inheritdoc />
    public ImmutableHashSet<string> InFlightCommands { get; init; } = ImmutableHashSet<string>.Empty;

    /// <inheritdoc />
    public ImmutableList<CommandHistoryEntry> CommandHistory { get; init; } = ImmutableList<CommandHistoryEntry>.Empty;
}