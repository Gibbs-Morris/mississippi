using Orleans;


namespace Crescent.Crescent.L2Tests.Domain.Counter.Commands;

/// <summary>
///     Command to decrement the counter by a specified amount.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.L2Tests.Domain.Counter.Commands.DecrementCounter")]
internal sealed record DecrementCounter
{
    /// <summary>
    ///     Gets the amount to decrement. Defaults to 1.
    /// </summary>
    [Id(0)]
    public int Amount { get; init; } = 1;
}