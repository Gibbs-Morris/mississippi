using Orleans;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Command to initialize a counter with a specific value.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Domain.InitializeCounter")]
internal sealed record InitializeCounter
{
    /// <summary>
    ///     Gets the initial value for the counter.
    /// </summary>
    [Id(0)]
    public int InitialValue { get; init; }
}
