using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Crescent.L2Tests;

/// <summary>
///     Event raised when the counter is reset.
/// </summary>
[EventStorageName("CRESCENT", "SAMPLE", "COUNTERRESET")]
[GenerateSerializer]
[Alias("Mississippi.Crescent.L2Tests.CounterReset")]
internal sealed record CounterReset
{
    /// <summary>
    ///     Gets the value the counter was reset to.
    /// </summary>
    [Id(0)]
    public int NewValue { get; init; }

    /// <summary>
    ///     Gets the value the counter had before reset.
    /// </summary>
    [Id(1)]
    public int PreviousValue { get; init; }
}