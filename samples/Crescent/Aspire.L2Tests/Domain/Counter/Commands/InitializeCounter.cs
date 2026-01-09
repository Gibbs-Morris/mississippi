using Orleans;


namespace Crescent.Aspire.L2Tests.Domain.Counter.Commands;

/// <summary>
///     Command to initialize a counter with a specific value.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.Aspire.L2Tests.Domain.Counter.Commands.InitializeCounter")]
internal sealed record InitializeCounter
{
    /// <summary>
    ///     Gets the initial value for the counter.
    /// </summary>
    [Id(0)]
    public int InitialValue { get; init; }
}
