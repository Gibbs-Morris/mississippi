using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Crescent.ConsoleApp.Counter.Events;

/// <summary>
///     Event raised when the counter is incremented.
/// </summary>
[EventStorageName("CRESCENT", "SAMPLE", "COUNTERINCREMENTED", version: 1)]
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Counter.Events.CounterIncremented")]
internal sealed record CounterIncremented
{
    /// <summary>
    ///     Gets the amount by which the counter was incremented.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; }
}