namespace Mississippi.DomainModeling.Abstractions.Builders;

/// <summary>
///     Builder contract for composing aggregate registrations in the runtime.
/// </summary>
/// <remarks>
///     <para>
///         Aggregates are the write-side domain surfaces. Each aggregate needs its root type,
///         event types, command handlers, reducers, event effects, and snapshot support registered.
///     </para>
///     <para>
///         Usage: <c>runtime.Aggregates(aggregates =&gt; aggregates.AddBankAccountAggregate())</c>.
///     </para>
/// </remarks>
public interface IAggregateBuilder
{
    /// <summary>
    ///     Validates the current aggregate builder configuration.
    /// </summary>
    void Validate();
}