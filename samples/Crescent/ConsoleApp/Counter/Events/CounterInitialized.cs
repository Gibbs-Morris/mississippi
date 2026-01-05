using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Crescent.ConsoleApp.Counter.Events;

/// <summary>
///     Event raised when a counter is initialized.
/// </summary>
[EventStorageName("CRESCENT", "SAMPLE", "COUNTERINITIALIZED", version: 1)]
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