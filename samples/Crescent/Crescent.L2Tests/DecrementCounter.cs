using Orleans;


namespace Mississippi.Crescent.L2Tests;

/// <summary>
///     Command to decrement the counter by a specified amount.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.Crescent.L2Tests.DecrementCounter")]
internal sealed record DecrementCounter
{
    /// <summary>
    ///     Gets the amount to decrement. Defaults to 1.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; } = 1;
}