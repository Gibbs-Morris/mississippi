using System.Collections.Immutable;

using Mississippi.Inlet.Client.Abstractions.Commands;


namespace Mississippi.Inlet.Client.Abstractions.State;

/// <summary>
///     Abstract base record for aggregate command state that provides standard command tracking properties.
/// </summary>
/// <remarks>
///     <para>
///         This base record provides the common implementation for <see cref="IAggregateCommandState" />:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Command history tracking via <see cref="CommandHistory" />.</description>
///         </item>
///         <item>
///             <description>In-flight command tracking via <see cref="InFlightCommands" />.</description>
///         </item>
///         <item>
///             <description>
///                 Error state from last failed command via <see cref="ErrorCode" /> and
///                 <see cref="ErrorMessage" />.
///             </description>
///         </item>
///         <item>
///             <description>Last command outcome via <see cref="LastCommandSucceeded" />.</description>
///         </item>
///     </list>
///     <para>
///         Derived types must define the static <c>FeatureKey</c> property required by
///         <see cref="IAggregateCommandState" /> and add any aggregate-specific state.
///     </para>
/// </remarks>
public abstract record AggregateCommandStateBase
{
    /// <summary>
    ///     Gets the history of command executions, ordered from oldest to newest.
    /// </summary>
    public ImmutableList<CommandHistoryEntry> CommandHistory { get; init; } = ImmutableList<CommandHistoryEntry>.Empty;

    /// <summary>
    ///     Gets the error code from the last failed command, or <c>null</c> if no error.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    ///     Gets the error message from the last failed command, or <c>null</c> if no error.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the set of command IDs currently in flight (executing but not yet completed).
    /// </summary>
    public ImmutableHashSet<string> InFlightCommands { get; init; } = ImmutableHashSet<string>.Empty;

    /// <summary>
    ///     Gets a value indicating whether any command is currently executing.
    /// </summary>
    public bool IsExecuting => !InFlightCommands.IsEmpty;

    /// <summary>
    ///     Gets a value indicating whether the last command succeeded.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the last command succeeded; <c>false</c> if it failed;
    ///     <c>null</c> if no command has completed yet or a command is in progress.
    /// </value>
    public bool? LastCommandSucceeded { get; init; }
}