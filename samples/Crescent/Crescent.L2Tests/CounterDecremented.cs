using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Event raised when the counter is decremented.
/// </summary>
[EventStorageName("CRESCENT", "SAMPLE", "COUNTERDECREMENTED")]
[GenerateSerializer]
[Alias("MississippiSamples.Crescent.L2Tests.CounterDecremented")]
internal sealed record CounterDecremented
{
    /// <summary>
    ///     Gets the amount by which the counter was decremented.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; }
}