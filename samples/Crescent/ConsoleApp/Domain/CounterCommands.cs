using Orleans;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Command to initialize a counter with a specific value.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Domain.InitializeCounter")]
public sealed record InitializeCounter
{
    /// <summary>
    ///     Gets the initial value for the counter.
    /// </summary>
    [Id(0)]
    public int InitialValue { get; init; }
}

/// <summary>
///     Command to increment the counter by a specified amount.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Domain.IncrementCounter")]
public sealed record IncrementCounter
{
    /// <summary>
    ///     Gets the amount to increment. Defaults to 1.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; } = 1;
}

/// <summary>
///     Command to decrement the counter by a specified amount.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Domain.DecrementCounter")]
public sealed record DecrementCounter
{
    /// <summary>
    ///     Gets the amount to decrement. Defaults to 1.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; } = 1;
}

/// <summary>
///     Command to reset the counter to a specified value.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Domain.ResetCounter")]
public sealed record ResetCounter
{
    /// <summary>
    ///     Gets the value to reset the counter to. Defaults to 0.
    /// </summary>
    [Id(0)]
    public int NewValue { get; init; }
}