using Mississippi.EventSourcing.Abstractions.Attributes;

using Orleans;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Event raised when a counter is initialized.
/// </summary>
[EventName("CRESCENT", "SAMPLE", "COUNTERINITIALIZED")]
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Domain.CounterInitialized")]
public sealed record CounterInitialized
{
    /// <summary>
    ///     Gets the initial value of the counter.
    /// </summary>
    [Id(0)]
    public int InitialValue { get; init; }
}

/// <summary>
///     Event raised when the counter is incremented.
/// </summary>
[EventName("CRESCENT", "SAMPLE", "COUNTERINCREMENTED")]
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Domain.CounterIncremented")]
public sealed record CounterIncremented
{
    /// <summary>
    ///     Gets the amount by which the counter was incremented.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; }
}

/// <summary>
///     Event raised when the counter is decremented.
/// </summary>
[EventName("CRESCENT", "SAMPLE", "COUNTERDECREMENTED")]
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Domain.CounterDecremented")]
public sealed record CounterDecremented
{
    /// <summary>
    ///     Gets the amount by which the counter was decremented.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; }
}

/// <summary>
///     Event raised when the counter is reset.
/// </summary>
[EventName("CRESCENT", "SAMPLE", "COUNTERRESET")]
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Domain.CounterReset")]
public sealed record CounterReset
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