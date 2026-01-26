namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Configuration options for aggregate effect processing.
/// </summary>
/// <remarks>
///     <para>
///         These options control how the generic aggregate grain processes event effects.
///         Effects can yield additional events, which are processed in an iterative loop.
///         The <see cref="MaxEffectIterations" /> property limits this loop to prevent
///         infinite cycles when effects continuously produce events.
///     </para>
///     <para>
///         The default value of 10 iterations is typically sufficient for legitimate effect
///         chains. If you find this limit is too restrictive, consider restructuring your
///         effect chain to avoid deep nesting before increasing the limit.
///     </para>
/// </remarks>
public sealed class AggregateEffectOptions
{
    /// <summary>
    ///     Gets the maximum number of effect iterations before the effect loop is terminated.
    /// </summary>
    /// <value>
    ///     The maximum number of effect iterations. Defaults to 10.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         This prevents infinite loops when effects continuously yield new events that
    ///         trigger other effects in a cycle. When the limit is reached, remaining pending
    ///         events are not processed, and a warning is logged with metrics recorded.
    ///     </para>
    ///     <para>
    ///         If this limit is reached during normal operation, it may indicate a design issue
    ///         with effects producing events in a cycle. Review your effect handlers to ensure
    ///         they terminate properly.
    ///     </para>
    /// </remarks>
    public int MaxEffectIterations { get; init; } = 10;
}
