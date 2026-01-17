using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Crescent.Crescent.L2Tests.Domain.Counter.Events;

/// <summary>
///     Event raised when the counter is reset.
/// </summary>
[EventStorageName("CRESCENT", "SAMPLE", "COUNTERRESET")]
[GenerateSerializer]
[Alias("Crescent.L2Tests.Domain.Counter.Events.CounterReset")]
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