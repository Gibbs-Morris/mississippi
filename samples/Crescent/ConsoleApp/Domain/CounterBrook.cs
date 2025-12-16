using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Attributes;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Brook definition for counter aggregates.
///     Provides compile-time type safety for referencing the counter event stream.
/// </summary>
[BrookName("CRESCENT", "SAMPLE", "COUNTER")]
public sealed class CounterBrook : IBrookDefinition
{
    /// <inheritdoc />
    public static string BrookName => "CRESCENT.SAMPLE.COUNTER";
}