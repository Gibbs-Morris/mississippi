using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Crescent.L2Tests;

/// <summary>
///     Event raised when a counter is initialized.
/// </summary>
[EventStorageName("CRESCENT", "SAMPLE", "COUNTERINITIALIZED")]
[GenerateSerializer]
[Alias("Mississippi.Crescent.L2Tests.CounterInitialized")]
internal sealed record CounterInitialized
{
    /// <summary>
    ///     Gets the initial value of the counter.
    /// </summary>
    [Id(0)]
    public int InitialValue { get; init; }
}