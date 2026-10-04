using Orleans;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Command to initialize a counter with a specific value.
/// </summary>
/// <param name="InitialValue">The initial value for the counter. Defaults to 0.</param>
[GenerateSerializer]
[Alias("MississippiSamples.Crescent.L2Tests.InitializeCounter")]
internal sealed record InitializeCounter([property: Id(0)] int InitialValue = 0);