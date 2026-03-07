using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Event raised when the counter is incremented.
/// </summary>
[EventStorageName("CRESCENT", "SAMPLE", "COUNTERINCREMENTED")]
[GenerateSerializer]
[Alias("Crescent.L2Tests.Domain.Counter.Events.CounterIncremented")]
internal sealed record CounterIncremented
{
    /// <summary>
    ///     Gets the amount by which the counter was incremented.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; }
}