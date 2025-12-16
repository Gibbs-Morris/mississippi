using Mississippi.EventSourcing.Abstractions.Attributes;

using Orleans;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Event raised when the counter is decremented.
/// </summary>
[EventName("CRESCENT", "SAMPLE", "COUNTERDECREMENTED")]
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Domain.CounterDecremented")]
internal sealed record CounterDecremented
{
    /// <summary>
    ///     Gets the amount by which the counter was decremented.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; }
}