using Mississippi.EventSourcing.Abstractions.Attributes;

using Orleans;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Event raised when the counter is incremented.
/// </summary>
[EventName("CRESCENT", "SAMPLE", "COUNTERINCREMENTED")]
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Domain.CounterIncremented")]
internal sealed record CounterIncremented
{
    /// <summary>
    ///     Gets the amount by which the counter was incremented.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; }
}