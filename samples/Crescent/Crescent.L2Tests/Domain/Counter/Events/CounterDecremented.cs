using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Crescent.Crescent.L2Tests.Domain.Counter.Events;

/// <summary>
///     Event raised when the counter is decremented.
/// </summary>
[EventStorageName("CRESCENT", "SAMPLE", "COUNTERDECREMENTED")]
[GenerateSerializer]
[Alias("Crescent.L2Tests.Domain.Counter.Events.CounterDecremented")]
internal sealed record CounterDecremented
{
    /// <summary>
    ///     Gets the amount by which the counter was decremented.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; }
}