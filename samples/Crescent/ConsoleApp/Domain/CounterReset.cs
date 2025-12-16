using Mississippi.EventSourcing.Abstractions.Attributes;

using Orleans;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Event raised when the counter is reset.
/// </summary>
[EventName("CRESCENT", "SAMPLE", "COUNTERRESET")]
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Domain.CounterReset")]
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
