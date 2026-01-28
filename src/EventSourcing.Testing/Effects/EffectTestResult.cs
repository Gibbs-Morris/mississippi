using System;
using System.Collections.Generic;


namespace Mississippi.EventSourcing.Testing.Effects;

/// <summary>
///     Base result class for effect test assertions.
/// </summary>
/// <remarks>
///     <para>
///         This class captures the commands dispatched during effect execution
///         and provides a base for fluent assertions. Domain-specific fixtures
///         can inherit from this class to add domain-specific assertion methods.
///     </para>
/// </remarks>
public class EffectTestResult
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EffectTestResult" /> class.
    /// </summary>
    /// <param name="dispatchedCommands">The commands dispatched during effect execution.</param>
    public EffectTestResult(
        IReadOnlyList<(Type AggregateType, string EntityId, object Command)> dispatchedCommands
    )
    {
        ArgumentNullException.ThrowIfNull(dispatchedCommands);
        DispatchedCommands = dispatchedCommands;
    }

    /// <summary>
    ///     Gets the number of commands dispatched.
    /// </summary>
    public int DispatchCount => DispatchedCommands.Count;

    /// <summary>
    ///     Gets the commands that were dispatched during effect execution.
    /// </summary>
    public IReadOnlyList<(Type AggregateType, string EntityId, object Command)> DispatchedCommands { get; }

    /// <summary>
    ///     Gets a value indicating whether any commands were dispatched.
    /// </summary>
    public bool HasDispatches => DispatchedCommands.Count > 0;
}