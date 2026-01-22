using System.Collections.Immutable;

using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Commands;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Inlet.Blazor.WebAssembly.Abstractions.State;

/// <summary>
///     Extends <see cref="IFeatureState" /> with command history tracking capabilities.
/// </summary>
/// <remarks>
///     <para>
///         Aggregate states that implement this interface automatically gain:
///         <list type="bullet">
///             <item>
///                 <description>In-flight command tracking (currently executing commands).</description>
///             </item>
///             <item>
///                 <description>Command history with configurable maximum entries (FIFO eviction).</description>
///             </item>
///             <item>
///                 <description>Correlation between executing, succeeded, and failed lifecycle actions.</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         Implementations should use <see cref="AggregateCommandStateReducers" /> helper methods
///         in their reducers to maintain this state correctly.
///     </para>
/// </remarks>
public interface IAggregateCommandState : IFeatureState
{
    /// <summary>
    ///     Gets the maximum number of command history entries to retain.
    /// </summary>
    /// <remarks>
    ///     When exceeded, oldest entries are removed (FIFO eviction).
    /// </remarks>
    const int DefaultMaxHistoryEntries = 200;

    /// <summary>
    ///     Gets the history of command executions, ordered from oldest to newest.
    /// </summary>
    ImmutableList<CommandHistoryEntry> CommandHistory { get; init; }

    /// <summary>
    ///     Gets the set of command IDs currently in flight (executing but not yet completed).
    /// </summary>
    ImmutableHashSet<string> InFlightCommands { get; init; }
}