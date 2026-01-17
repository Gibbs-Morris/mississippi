using Orleans;


namespace Crescent.Crescent.L2Tests.Domain.Counter.Commands;

/// <summary>
///     Command to increment the counter by a specified amount.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.L2Tests.Domain.Counter.Commands.IncrementCounter")]
internal sealed record IncrementCounter
{
    /// <summary>
    ///     Gets the amount to increment. Defaults to 1.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; } = 1;
}