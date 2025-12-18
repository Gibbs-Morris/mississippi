using Mississippi.EventSourcing.Abstractions.Attributes;

using Orleans;


namespace Crescent.ConsoleApp.Counter.Events;

/// <summary>
///     Event raised when a counter is initialized.
/// </summary>
[EventName("CRESCENT", "SAMPLE", "COUNTERINITIALIZED")]
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Counter.Events.CounterInitialized")]
internal sealed record CounterInitialized
{
    /// <summary>
    ///     Gets the initial value of the counter.
    /// </summary>
    [Id(0)]
    public int InitialValue { get; init; }
}
