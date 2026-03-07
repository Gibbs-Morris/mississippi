using Orleans;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Command to increment the counter by a specified amount.
/// </summary>
[GenerateSerializer]
[Alias("MississippiSamples.Crescent.L2Tests.IncrementCounter")]
internal sealed record IncrementCounter
{
    /// <summary>
    ///     Gets the amount to increment. Defaults to 1.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; } = 1;
}