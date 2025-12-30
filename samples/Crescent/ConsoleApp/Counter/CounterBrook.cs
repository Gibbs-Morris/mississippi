using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Crescent.ConsoleApp.Counter;

/// <summary>
///     Brook definition for counter aggregates.
///     Provides compile-time type safety for referencing the counter event stream.
/// </summary>
[BrookName("CRESCENT", "SAMPLE", "COUNTER")]
internal sealed class CounterBrook : IBrookDefinition
{
    /// <inheritdoc />
    public static string BrookName => "CRESCENT.SAMPLE.COUNTER";
}