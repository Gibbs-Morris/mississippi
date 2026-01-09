using Orleans;


namespace Crescent.L2Tests.Domain.Counter.Commands;

/// <summary>
///     Command to initialize a counter with a specific value.
/// </summary>
/// <param name="InitialValue">The initial value for the counter. Defaults to 0.</param>
[GenerateSerializer]
[Alias("Crescent.L2Tests.Domain.Counter.Commands.InitializeCounter")]
internal sealed record InitializeCounter(
    [property: Id(0)] int InitialValue = 0
);
