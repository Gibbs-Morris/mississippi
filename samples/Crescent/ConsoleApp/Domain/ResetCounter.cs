using Orleans;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Command to reset the counter to a specified value.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Domain.ResetCounter")]
internal sealed record ResetCounter
{
    /// <summary>
    ///     Gets the value to reset the counter to. Defaults to 0.
    /// </summary>
    [Id(0)]
    public int NewValue { get; init; }
}